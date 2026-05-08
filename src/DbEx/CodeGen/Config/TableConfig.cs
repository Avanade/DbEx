namespace DbEx.CodeGen.Config;

/// <summary>
/// Provides the database table code-generation configuration.
/// </summary>
[CodeGenClass("Table", Title = "Database table configuration.")]
[CodeGenCategory("Primary", Title = "Provides the _primary_ configuration.")]
[CodeGenCategory("Columns", Title = "Provides the configuration for the database columns.")]
[CodeGenCategory("Entity Framework", Title = "Provides the configuration for the Entity Framework (EF) capabilities.")]
[CodeGenCategory("By-Convention", Title = "Provides the by-convention column-naming configuration.")]
[CodeGenCategory("Collections", Title = "Provides the collections configuration.")]
public class TableConfig : ConfigBase<CodeGenConfig, CodeGenConfig>, IByConventionColumnNames, IByConventionColumns
{
    /// <summary>
    /// Gets or sets the database table name.
    /// </summary>
    [JsonPropertyName("name")]
    [CodeGenProperty("Primary", Title = "The database table name.", IsMandatory = true)]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the database schema name (where applicable).
    /// </summary>
    [JsonPropertyName("schema")]
    [CodeGenProperty("Primary", Title = "The database schema name (where applicable).", Description = "Defaults to the root '{Schema}' configuration.")]
    public string? Schema { get; set; }

    /// <summary>
    /// Gets or sets the list of database columns to include explicitly.
    /// </summary>
    [JsonPropertyName("includeColumns")]
    [CodeGenPropertyCollection("Columns", Title = "The list of database columns to include specifically.", Description = "All columns are included by default; this provides a means to simply select those for inclusion.")]
    public List<string>? IncludeColumns { get; set; }

    /// <summary>
    /// Gets or sets the list of database column names to exclude explicitly.
    /// </summary>
    [JsonPropertyName("excludeColumns")]
    [CodeGenPropertyCollection("Columns", Title = "The list of database columns to exclude specifically.", Description = "All columns are included by default; this provides a means to simply select those for exclusion. A single item of '*' indicates all columns are to be excluded.")]
    public List<string>? ExcludeColumns { get; set; }

    /// <summary>
    /// Indicates the default entity-framework code-generation choice.
    /// </summary>
    [JsonPropertyName("efModel")]
    [CodeGenProperty("Entity Framework", Title = "The entity-framework code-generation choice.", Description = "Defaults to parent '{EfModel}'. A 'Yes' indicates combination of 'ModelOnly' and 'ModelBuilderOnly'.", Options = ["Yes", "No", "ModelOnly", "ModelBuilderOnly"])]
    public string? EfModel { get; set; }

    /// <summary>
    /// Gets or sets the name of the entity-framework model associated with this instance.
    /// </summary>
    [JsonPropertyName("efModelName")]
    [CodeGenProperty("Entity Framework", Title = "The name of the entity-framework model associated with this instance.", Description = "Defaults to the database table's .NET formatted name.")]
    public string? EfModelName { get; set; }

    #region By-Convention

    /// <inheritdoc/>
    [JsonPropertyName("columnNameIsDeleted")]
    [CodeGenProperty("By-Convention", Title = "The default 'IsDeleted' column name.", Description = "Defaults to 'IsDeleted'.")]
    public string? ColumnNameIsDeleted { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("columnNameTenantId")]
    [CodeGenProperty("By-Convention", Title = "The default 'TenantId' column name.", Description = "Defaults to 'TenantId'.")]
    public string? ColumnNameTenantId { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("columnNameRowVersion")]
    [CodeGenProperty("By-Convention", Title = "The default 'RowVersion' column name.", Description = "Defaults to 'RowVersion'.")]
    public string? ColumnNameRowVersion { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("columnNameCreatedBy")]
    [CodeGenProperty("By-Convention", Title = "The default 'CreatedBy' column name.", Description = "Defaults to 'CreatedBy'.")]
    public string? ColumnNameCreatedBy { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("columnNameCreatedOn")]
    [CodeGenProperty("By-Convention", Title = "The default 'CreatedOn' column name.", Description = "Defaults to 'CreatedOn'.")]
    public string? ColumnNameCreatedOn { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("columnNameUpdatedBy")]
    [CodeGenProperty("By-Convention", Title = "The default 'UpdatedBy' column name.", Description = "Defaults to 'UpdatedBy'.")]
    public string? ColumnNameUpdatedBy { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("columnNameUpdatedOn")]
    [CodeGenProperty("By-Convention", Title = "The default 'UpdatedOn' column name.", Description = "Defaults to 'UpdatedOn'.")]
    public string? ColumnNameUpdatedOn { get; set; }

    /// <inheritdoc/>
    public ColumnConfig? ColumnIsDeleted => Columns!.SingleOrDefault(x => x.Name == ColumnNameIsDeleted);

    /// <inheritdoc/>
    public ColumnConfig? ColumnTenantId => Columns!.SingleOrDefault(x => x.Name == ColumnNameTenantId);

    /// <inheritdoc/>
    public ColumnConfig? ColumnRowVersion => Columns!.SingleOrDefault(x => x.Name == ColumnNameRowVersion);

    /// <inheritdoc/>
    public ColumnConfig? ColumnCreatedBy => Columns!.SingleOrDefault(x => x.Name == ColumnNameCreatedBy);

    /// <inheritdoc/>
    public ColumnConfig? ColumnCreatedOn => Columns!.SingleOrDefault(x => x.Name == ColumnNameCreatedOn);

    /// <inheritdoc/>
    public ColumnConfig? ColumnUpdatedBy => Columns!.SingleOrDefault(x => x.Name == ColumnNameUpdatedBy);

    /// <inheritdoc/>
    public ColumnConfig? ColumnUpdatedOn => Columns!.SingleOrDefault(x => x.Name == ColumnNameUpdatedOn);

    /// <summary>
    /// Indicates whether the 'IsDeleted' column exists.
    /// </summary>
    public bool HasColumnIsDeleted => ColumnIsDeleted is not null;

    /// <summary>
    /// Indicates whether the 'TenantId' column exists.
    /// </summary>
    public bool HasColumnTenantId => ColumnTenantId is not null;

    /// <summary>
    /// Indicates whether the 'RowVersion' column exists.
    /// </summary>
    public bool HasColumnRowVersion => ColumnRowVersion is not null;

    /// <summary>
    /// Indicates whether the 'CreatedOn' column exists.
    /// </summary>
    public bool HasColumnCreatedOn => ColumnCreatedOn is not null;

    /// <summary>
    /// Indicates whether the 'CreatedBy' column exists.
    /// </summary>
    public bool HasColumnCreatedBy => ColumnCreatedBy is not null;

    /// <summary>
    /// Indicates whether the 'UpdatedOn' column exists.
    /// </summary>
    public bool HasColumnUpdatedOn => ColumnUpdatedOn is not null;

    /// <summary>
    /// Indicates whether the 'UpdatedBy' column exists.
    /// </summary>
    public bool HasColumnUpdatedBy => ColumnUpdatedBy is not null;

    /// <summary>
    /// Indicates whether all the audit columns exist.
    /// </summary>
    public bool HasAllAuditColumns => ColumnCreatedBy is not null && ColumnCreatedOn is not null && ColumnUpdatedBy is not null && ColumnUpdatedOn is not null;

    /// <inheritdoc/>
    public bool HasAtLeastOneAuditColumn => ColumnCreatedBy is not null || ColumnCreatedOn is not null || ColumnUpdatedBy is not null || ColumnUpdatedOn is not null;

    #endregion

    /// <summary>
    /// Gets the list of configured columns.
    /// </summary>
    [JsonPropertyName("columns")]
    [CodeGenPropertyCollection("Collections", Title = "The database column collection configuration.", IsImportant = true,
        Description = "This collection is optional. It is used to specifically declare and/or override the column configurations. This bypasses the {IncludeColumns} and {ExcludesColumns} lists.")]
    public List<ColumnConfig>? Columns { get; set; }

    /// <summary>
    /// Gets the corresponding (actual) database table configuration.
    /// </summary>
    public DbTableSchema? DbTable { get; private set; }

    /// <summary>
    /// Gets the list of configured columns that represent the primary key.
    /// </summary>
    public List<ColumnConfig> PrimaryKeyColumns => [.. Columns!.Where(x => x.DbColumn!.IsPrimaryKey)];

    /// <summary>
    /// Indicates whether there is a single primary key column that is an identifier (i.e. named with suffix 'Id' (any case)).
    /// </summary>
    public bool HasPrimaryKeyIdentifier => DbTable!.HasPrimaryKeyIdentifier;

    /// <summary>
    /// Gets the column configuration that represents the primary key identifier, if one exists.
    /// </summary>
    public ColumnConfig? PrimaryKeyIdentifierColumn => Columns!.SingleOrDefault(x => x.DbColumn!.IsPrimaryKeyIdentifier);

    /// <summary>
    /// Gets the list of configured columns that do not represent the primary key, and are not audit, tenant-id, row-version or soft-delete related columns (i.e. the "standard" columns).
    /// </summary>
    public List<ColumnConfig> StandardColumns => [.. Columns!.Where(x => !x.DbColumn!.IsPrimaryKey && !x.DbColumn!.IsCreatedAudit && !x.DbColumn!.IsUpdatedAudit && !x.DbColumn!.IsRowVersion && !x.DbColumn!.IsTenantId && !x.DbColumn!.IsIsDeleted)];

    /// <summary>
    /// Gets the list of configured columns that are convention-based; i.e. those that represent audit, tenant-id, row-version or soft-delete related columns based on standard naming conventions (or explicit configuration).
    /// </summary>
    public List<ColumnConfig> ConventionColumns => [.. Columns!.Where(x => x.DbColumn!.IsCreatedAudit || x.DbColumn!.IsUpdatedAudit || x.DbColumn!.IsRowVersion || x.DbColumn!.IsTenantId || x.DbColumn!.IsIsDeleted)];

    /// <summary>
    /// Gets the reference-data configuration for this table, if applicable (i.e. if the table is considered reference data based on the presence of conventionally named 'Code' and 'Text' columns that are not primary keys, and are of type 'string').
    /// </summary>
    public RefDataConfig RefData { get; } = new();

    /// <inheritdoc/>
    protected async override Task PrepareAsync()
    {
        Schema = DefaultWhereNull(Schema, () => Parent!.Schema);
        DbTable = Root!.DbTables.SingleOrDefault(x => x.Name == Name && x.Schema == Schema) ?? throw new CodeGenException(this, nameof(Name), $"Table '{Root!.Migrator.SchemaConfig.ToFullyQualifiedTableName(Schema, Name!)}' not found in database.");
        EfModel = DefaultWhereNull(EfModel, () => Parent!.EfModel);
        EfModelName = DefaultWhereNull(EfModelName, () => DbTable.DotNetName);

        // Default the by-convention properties.
        ColumnNameIsDeleted = DefaultWhereNull(ColumnNameIsDeleted, () => Parent!.ColumnNameIsDeleted);
        ColumnNameTenantId = DefaultWhereNull(ColumnNameTenantId, () => Parent!.ColumnNameTenantId);
        ColumnNameRowVersion = DefaultWhereNull(ColumnNameRowVersion, () => Parent!.ColumnNameRowVersion);
        ColumnNameCreatedBy = DefaultWhereNull(ColumnNameCreatedBy, () => Parent!.ColumnNameCreatedBy);
        ColumnNameCreatedOn = DefaultWhereNull(ColumnNameCreatedOn, () => Parent!.ColumnNameCreatedOn);
        ColumnNameUpdatedBy = DefaultWhereNull(ColumnNameUpdatedBy, () => Parent!.ColumnNameUpdatedBy);
        ColumnNameUpdatedOn = DefaultWhereNull(ColumnNameUpdatedOn, () => Parent!.ColumnNameUpdatedOn);

        // Merge configured (always included) and selected columns to build the final column configuration collection for the table. This approach attempts to keep the declared order of the columns in the database.
        var columns = new List<ColumnConfig>();
        foreach (var column in DbTable.Columns)
        {
            var configuredColumn = Columns?.SingleOrDefault(x => x.Name == column.Name);
            if (configuredColumn is not null)
                columns.Add(configuredColumn);
            else
            {
                if (ExcludeColumns is not null && ExcludeColumns.Count == 1 && ExcludeColumns[0] == "*")
                    continue;

                if ((ExcludeColumns is null || !ExcludeColumns.Contains(column.Name)) && (IncludeColumns is null || IncludeColumns.Contains(column.Name)))
                    columns.Add(new ColumnConfig { Name = column.Name, DbColumn = column });
            }
        }

        // Prepare the column configurations (e.g. to resolve the column type, etc.) and replace as final.
        Columns = await PrepareCollectionAsync(columns).ConfigureAwait(false);

        // Prepare the reference-data configuration.
        await RefData.PrepareAsync(Root!, this).ConfigureAwait(false);
    }
}