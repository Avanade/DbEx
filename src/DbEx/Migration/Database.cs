namespace DbEx.Migration;

/// <summary>
/// Provides the common/base database access functionality.
/// </summary>
/// <typeparam name="TConnection">The <see cref="DbConnection"/> <see cref="Type"/>.</typeparam>
/// <param name="create">The function to create the <typeparamref name="TConnection"/> <see cref="DbConnection"/>.</param>
/// <param name="provider">The underlying <see cref="DbProviderFactory"/>.</param>
public class Database<TConnection>(Func<TConnection> create, DbProviderFactory provider) : IDatabase where TConnection : DbConnection
{
    private readonly Func<TConnection> _dbConnCreate = create.ThrowIfNull(nameof(create));
    private TConnection? _dbConn;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <inheritdoc/>
    public DbProviderFactory Provider { get; } = provider.ThrowIfNull(nameof(provider));

    /// <inheritdoc/>
    public DbConnection GetConnection() => _dbConn is not null ? _dbConn : GetConnectionAsync().GetAwaiter().GetResult();

    /// <inheritdoc/>
    async Task<DbConnection> IDatabase.GetConnectionAsync(CancellationToken cancellationToken) => await GetConnectionAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<TConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_dbConn == null)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_dbConn != null)
                    return _dbConn;

                _dbConn = _dbConnCreate() ?? throw new InvalidOperationException($"The create function must create a valid {nameof(TConnection)} instance.");
                await _dbConn.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                _dbConn?.Dispose();
                _dbConn = null;
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        return _dbConn;
    }

    /// <inheritdoc/>
    public DatabaseCommand StoredProcedure(string storedProcedure) => new(this, System.Data.CommandType.StoredProcedure, storedProcedure.ThrowIfNull(nameof(storedProcedure)));

    /// <inheritdoc/>
    public DatabaseCommand SqlStatement(string sqlStatement) => new(this, System.Data.CommandType.Text, sqlStatement.ThrowIfNull(nameof(sqlStatement)));

    /// <inheritdoc/>
    public async Task<List<DbTableSchema>> SelectSchemaAsync(DatabaseMigrationBase migration, CancellationToken cancellationToken = default)
    {
        migration.ThrowIfNull(nameof(migration));
        migration.PreExecutionInitialization();

        var tables = new List<DbTableSchema>();
        DbTableSchema? table = null;

        var refDataPredicate = new Func<DbTableSchema, bool>(t => t.Columns.Any(c => c.Name == migration.Args.RefDataCodeColumnName! && !c.IsPrimaryKey && c.DotNetType == "string") && t.Columns.Any(c => c.Name == migration.Args.RefDataTextColumnName && !c.IsPrimaryKey && c.DotNetType == "string"));

        // Get all the tables and their columns.
        var probeAssemblies = new[] { migration.SchemaConfig.GetType().Assembly, typeof(DatabaseExtensions).Assembly };
        using var sr = DatabaseMigrationBase.GetRequiredResourcesStreamReader("SelectTableAndColumns.sql", probeAssemblies);
        await SqlStatement(await ReadSqlAsync(migration, sr, cancellationToken).ConfigureAwait(false)).SelectQueryAsync(dr =>
        {
            if (!migration.SchemaConfig.SupportsSchema && dr.GetValue<string>("TABLE_SCHEMA") != migration.DatabaseName)
                return 0;

            var dt = new DbTableSchema(migration, dr.GetValue<string>("TABLE_SCHEMA"), dr.GetValue<string>("TABLE_NAME")!)
            {
                IsAView = dr.GetValue<string>("TABLE_TYPE") == "VIEW"
            };

            if (table == null || table.Schema != dt.Schema || table.Name != dt.Name)
                tables.Add(table = dt);

            var dc = migration.SchemaConfig.CreateColumnFromInformationSchema(table, dr);
            dc.IsCreatedOn = dc.Name == migration.Args?.CreatedOnColumnName;
            dc.IsCreatedBy = dc.Name == migration.Args?.CreatedByColumnName;
            dc.IsUpdatedOn = dc.Name == migration.Args?.UpdatedOnColumnName;
            dc.IsUpdatedBy = dc.Name == migration.Args?.UpdatedByColumnName;
            dc.IsTenantId = dc.Name == migration.Args?.TenantIdColumnName;
            dc.IsRowVersion = dc.Name == migration.Args?.RowVersionColumnName;
            dc.IsIsDeleted = dc.Name == migration.Args?.IsDeletedColumnName;

            table.Columns.Add(dc);
            return 0;
        }, cancellationToken).ConfigureAwait(false);

        // Exit where no tables initially found.
        if (tables.Count == 0)
            return tables;

        // Configure all the single column primary and unique constraints.
        using var sr2 = DatabaseMigrationBase.GetRequiredResourcesStreamReader("SelectTablePrimaryKey.sql", probeAssemblies);
        var pks = await SqlStatement(await ReadSqlAsync(migration, sr2, cancellationToken).ConfigureAwait(false)).SelectQueryAsync(dr => new
        {
            ConstraintName = dr.GetValue<string>("CONSTRAINT_NAME"),
            TableSchema = dr.GetValue<string>("TABLE_SCHEMA"),
            TableName = dr.GetValue<string>("TABLE_NAME"),
            TableColumnName = dr.GetValue<string>("COLUMN_NAME"),
            IsPrimaryKey = dr.GetValue<string?>("CONSTRAINT_TYPE")?.StartsWith("PRIMARY", StringComparison.OrdinalIgnoreCase) ?? false
        }, cancellationToken).ConfigureAwait(false);

        if (!migration.SchemaConfig.SupportsSchema)
            pks = [.. pks.Where(x => x.TableSchema == migration.DatabaseName)];

        foreach (var grp in pks.GroupBy(x => new { x.ConstraintName, x.TableSchema, x.TableName }))
        {
            // Only single column unique columns are supported.
            if (grp.Count() > 1 && !grp.First().IsPrimaryKey)
                continue;

            // Set the column flags as appropriate.
            foreach (var pk in grp)
            {
                var col = (from t in tables
                           from c in t.Columns
                           where (!migration.SchemaConfig.SupportsSchema || t.Schema == pk.TableSchema) && t.Name == pk.TableName && c.Name == pk.TableColumnName
                           select c).SingleOrDefault();

                if (col == null)
                    continue;

                if (pk.IsPrimaryKey)
                {
                    col.IsPrimaryKey = true;
                    col.IsPrimaryKeyIdentifier = col.Name.EndsWith("Id", StringComparison.InvariantCultureIgnoreCase);
                    if (!col.IsIdentity)
                        col.IsIdentity = col.DefaultValue != null;
                }
                else
                    col.IsUnique = true;
            }
        }

        // Determine whether a table is considered reference data.
        foreach (var t in tables)
        {
            t.IsRefData = t.HasPrimaryKeyIdentifier && refDataPredicate(t);
            if (t.IsRefData)
                t.RefDataCodeColumn = t.Columns.Where(x => x.Name == migration.Args.RefDataCodeColumnName).SingleOrDefault();
        }

        // Load any additional configuration specific to the database provider.
        await migration.SchemaConfig.LoadAdditionalInformationSchema(this, tables, cancellationToken).ConfigureAwait(false);

        // Attempt to infer foreign key reference data relationship where not explicitly specified. 
        foreach (var t in tables)
        {
            foreach (var c in t.Columns.Where(x => !x.IsPrimaryKey))
            {
                if (c.ForeignTable != null)
                {
                    if (c.IsForeignRefData)
                    {
                        c.ForeignRefDataCodeColumn = migration.Args.RefDataCodeColumnName;
                        if (c.Name.EndsWith(migration.Args.IdColumnNameSuffix!, StringComparison.Ordinal))
                            c.DotNetCleanedName = DbTableSchema.CreateDotNetName(c.Name[0..^migration.Args.IdColumnNameSuffix!.Length]);
                    }

                    continue;
                }

                if (!c.Name.EndsWith(migration.Args.IdColumnNameSuffix!, StringComparison.Ordinal))
                    continue;

                // Find table with same name as column in any schema that is considered reference data and has a single primary key.
                var fk = tables.Where(x => x != t && x.Name == c.Name[0..^migration.Args.IdColumnNameSuffix!.Length] && x.IsRefData && x.PrimaryKeyColumns.Count == 1).FirstOrDefault();
                if (fk == null)
                    continue;

                c.ForeignSchema = fk.Schema;
                c.ForeignTable = fk.Name;
                c.ForeignColumn = fk.PrimaryKeyColumns[0].Name;
                c.IsForeignRefData = true;
                c.ForeignRefDataCodeColumn = migration.Args.RefDataCodeColumnName;
                c.DotNetCleanedName = DbTableSchema.CreateDotNetName(c.Name[0..^migration.Args.IdColumnNameSuffix!.Length]);
            }
        }

        // Attempt to infer if a reference data column where not explicitly specified.
        foreach (var t in tables)
        {
            foreach (var c in t.Columns.Where(x => !x.IsPrimaryKey))
            {
                if (c.IsForeignRefData)
                {
                    c.IsRefData = true;
                    continue;
                }

                // Find possible name by removing suffix by-convention.
                string name;
                if (c.Name.EndsWith(migration.Args.IdColumnNameSuffix!, StringComparison.Ordinal))
                    name = c.Name[0..^migration.Args.IdColumnNameSuffix!.Length];
                else if (c.Name.EndsWith(migration.Args.CodeColumnNameSuffix!, StringComparison.Ordinal))
                    name = c.Name[0..^migration.Args.CodeColumnNameSuffix!.Length];
                else
                    continue;

                // Is there a table match of same name that is considered reference data; if so, consider ref data.
                if (tables.Any(x => x.Name == name && x.Schema == t.Schema && x.IsRefData))
                {
                    c.IsRefData = true;
                    c.DotNetCleanedName = DbTableSchema.CreateDotNetName(name);
                }
            }
        }

        return tables;
    }

    /// <summary>
    /// Gets the SQL statement from the embedded resource stream
    /// </summary>
    private async static Task<string> ReadSqlAsync(DatabaseMigrationBase migration, StreamReader sr, CancellationToken cancellationToken)
    {
#if NET7_0_OR_GREATER
        var sql = await sr.ThrowIfNull(nameof(sr)).ReadToEndAsync(cancellationToken).ConfigureAwait(false);
#else
        var sql = await sr.ThrowIfNull(nameof(sr)).ReadToEndAsync().ConfigureAwait(false);
#endif
        return sql.Replace("{{DatabaseName}}", migration.DatabaseName);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose of the resources.
    /// </summary>
    /// <param name="disposing">Indicates whether to dispose.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing && _dbConn != null)
        {
            _dbConn.Dispose();
            _dbConn = null;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose of the resources asynchronously.
    /// </summary>
    public virtual async ValueTask DisposeAsyncCore()
    {
        if (_dbConn != null)
        {
            await _dbConn.DisposeAsync().ConfigureAwait(false);
            _dbConn = null;
        }

        Dispose();
    }
}