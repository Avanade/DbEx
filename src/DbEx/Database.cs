// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace DbEx
{
    /// <summary>
    /// Provides the base database access functionality.
    /// </summary>
    /// <typeparam name="TConnection">The <see cref="DbConnection"/> <see cref="Type"/>.</typeparam>
    public abstract class Database<TConnection> : IDatabase, IDisposable where TConnection : DbConnection
    {
        private readonly Func<TConnection> _dbConnCreate;
        private TConnection? _dbConn;

        /// <summary>
        /// Initializes a new instance of the <see cref="Database{TConn}"/> class.
        /// </summary>
        /// <param name="create">The function to create the <typeparamref name="TConnection"/> <see cref="DbConnection"/>.</param>
        protected Database(Func<TConnection> create) => _dbConnCreate = create ?? throw new ArgumentNullException(nameof(create));

        /// <summary>
        /// Gets the <typeparamref name="TConnection"/> <see cref="DbConnection"/>.
        /// </summary>
        /// <remarks>The connection is created and opened on first use, and closed on <see cref="IDisposable.Dispose()"/>.</remarks>
        public async Task<TConnection> GetConnectionAsync()
        {
            if (_dbConn == null)
            {
                _dbConn = _dbConnCreate() ?? throw new InvalidOperationException($"The create function must create a valid {nameof(TConnection)} instance.");
                await _dbConn.OpenAsync().ConfigureAwait(false);
            }

            return _dbConn;
        }

        /// <inheritdoc/>
        async Task<DbConnection> IDatabase.GetConnectionAsync() => await GetConnectionAsync().ConfigureAwait(false);

        /// <inheritdoc/>
        public DatabaseCommand StoredProcedure(string storedProcedure, Action<DatabaseParameterCollection>? parameters = null)
            => new(this, CommandType.StoredProcedure, storedProcedure ?? throw new ArgumentNullException(nameof(storedProcedure)), parameters);

        /// <inheritdoc/>
        public DatabaseCommand SqlStatement(string sqlStatement, Action<DatabaseParameterCollection>? parameters = null)
            => new(this, CommandType.Text, sqlStatement ?? throw new ArgumentNullException(nameof(sqlStatement)), parameters);

        /// <inheritdoc/>
        public virtual void OnDbException(DbException dbex) { }

        /// <inheritdoc/>
        public virtual async Task<List<Schema.DbTable>> SelectSchemaAsync(Schema.DbSchemaArgs? args)
        {
            args ??= new Schema.DbSchemaArgs();
            var tables = new List<Schema.DbTable>();
            Schema.DbTable? table = null;

            // Get all the tables and their columns.
            using var sr = StreamLocator.GetResourcesStreamReader("SelectTableAndColumns.sql")!;
            await SqlStatement(await sr.ReadToEndAsync().ConfigureAwait(false)).SelectAsync(new DatabaseRecordMapper(dr =>
            {
                var dt = new Schema.DbTable 
                { 
                    Name = dr.GetValue<string>("TABLE_NAME"), 
                    Schema = dr.GetValue<string>("TABLE_SCHEMA"), 
                    IsAView = dr.GetValue<string>("TABLE_TYPE") == "VIEW" 
                };

                if (table == null || table.Schema != dt.Schema || table.Name != dt.Name)
                    tables.Add(table = dt);

                var dc = new Schema.DbColumn
                {
                    Name = dr.GetValue<string>("COLUMN_NAME"),
                    Type = dr.GetValue<string>("DATA_TYPE"),
                    IsNullable = dr.GetValue<string>("IS_NULLABLE").ToUpperInvariant() == "YES",
                    Length = dr.GetValue<int?>("CHARACTER_MAXIMUM_LENGTH"),
                    Precision = dr.GetValue<byte?>("NUMERIC_PRECISION") ?? dr.GetValue<short?>("DATETIME_PRECISION"),
                    Scale = dr.GetValue<int?>("NUMERIC_SCALE"),
                    DefaultValue = dr.GetValue<string>("COLUMN_DEFAULT")
                };

                table.Columns.Add(dc);
            })).ConfigureAwait(false);

            // Exit where no tables initially found.
            if (tables.Count == 0)
                return tables;

            // Determine whether a table is considered reference data.
            foreach (var t in tables)
            {
                t.IsRefData = args.RefDataPredicate(t);
            }

            // Configure all the single column primary and unique constraints.
            using var sr2 = StreamLocator.GetResourcesStreamReader("SelectTablePrimaryKey.sql")!;
            var pks = await SqlStatement(await sr2.ReadToEndAsync().ConfigureAwait(false)).SelectAsync(dr => new
            {
                ConstraintName = dr.GetValue<string>("CONSTRAINT_NAME"),
                TableSchema = dr.GetValue<string>("TABLE_SCHEMA"),
                TableName = dr.GetValue<string>("TABLE_NAME"),
                TableColumnName = dr.GetValue<string>("COLUMN_NAME"),
                IsPrimaryKey = dr.GetValue<string>("CONSTRAINT_TYPE").StartsWith("PRIMARY", StringComparison.InvariantCultureIgnoreCase)
            }).ConfigureAwait(false);

            foreach (var grp in pks.GroupBy(x => x.ConstraintName))
            {
                // Only single column unique columns are supported.
                if (grp.Count() > 1 && !grp.First().IsPrimaryKey)
                    continue;

                // Set the column flags as appropriate.
                foreach (var pk in grp)
                {
                    var col = (from t in tables
                               from c in t.Columns
                               where t.Schema == pk.TableSchema && t.Name == pk.TableName && c.Name == pk.TableColumnName
                               select c).Single();

                    if (pk.IsPrimaryKey)
                    {
                        col.IsPrimaryKey = true;
                        col.IsIdentity = col.DefaultValue != null;
                    }
                    else
                        col.IsUnique = true;
                }
            }

            // Configure all the single column foreign keys.
            using var sr3 = StreamLocator.GetResourcesStreamReader("SelectTableForeignKeys.sql")!;
            var fks = await SqlStatement(await sr3.ReadToEndAsync().ConfigureAwait(false)).SelectAsync(dr => new
            {
                ConstraintName = dr.GetValue<string>("FK_CONSTRAINT_NAME"),
                TableSchema = dr.GetValue<string>("FK_SCHEMA_NAME"),
                TableName = dr.GetValue<string>("FK_TABLE_NAME"),
                TableColumnName = dr.GetValue<string>("FK_COLUMN_NAME"),
                ForeignSchema = dr.GetValue<string>("UQ_SCHEMA_NAME"),
                ForeignTable = dr.GetValue<string>("UQ_TABLE_NAME"),
                ForiegnColumn = dr.GetValue<string>("UQ_COLUMN_NAME")
            }).ConfigureAwait(false);
            
            foreach (var grp in fks.GroupBy(x => x.ConstraintName).Where(x => x.Count() == 1))
            {
                var fk = grp.Single();
                var r = (from t in tables
                         from c in t.Columns
                         where t.Schema == fk.TableSchema && t.Name == fk.TableName && c.Name == fk.TableColumnName
                         select (t, c)).Single();

                r.c.ForeignSchema = fk.ForeignSchema;
                r.c.ForeignTable = fk.ForeignTable;
                r.c.ForeignColumn = fk.ForiegnColumn;
                r.c.IsForeignRefData = r.t.IsRefData;
            }

            return tables;
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
    }
}