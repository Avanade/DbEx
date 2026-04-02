namespace DbEx.CodeGen.Config;

/// <summary>
/// Provides the root code-generation configuration.
/// </summary>
[CodeGenClass("CodeGeneration", Title = "Database-driven by-convention code-generation.")]
[CodeGenCategory("Primary", Title = "Provides the _primary_ configuration.")]
[CodeGenCategory("Entity Framework", Title = "Provides the configuration for the Entity Framework (EF) capabilities.")]
[CodeGenCategory("Outbox", Title = "Provides the configuration for the transactional-outbox capabilities.")]
[CodeGenCategory("Paths", Title = "Provides the configuration for the paths used in code generation.")]
[CodeGenCategory("By-Convention", Title = "Provides the by-convention column-naming configuration.")]
[CodeGenCategory("Collections", Title = "Provides the collections configuration.")]
public class CodeGenConfig : ConfigRootBase<CodeGenConfig>, IByConventionColumnNames
{
    private DatabaseMigrationBase? _migrator;
    private List<DbTableSchema>? _dbTables;

    /// <summary>
    /// Gets the owning <see cref="DatabaseMigrationBase"/>.
    /// </summary>
    public DatabaseMigrationBase Migrator { get => _migrator ?? throw new CodeGenException("The 'Migrator' has not been set."); private set => _migrator = value; }

    /// <summary>
    /// Gets or sets the default database schema name.
    /// </summary>
    [JsonPropertyName("schema")]
    [CodeGenProperty("Primary", Title = "The default database schema name.", IsImportant = true)]
    public string? Schema { get; set; }

    /// <summary>
    /// Gets or sets the .NET domain name.
    /// </summary>
    [JsonPropertyName("domain")]
    [CodeGenProperty("Primary", Title = "The domain name.", IsImportant = true, Description = "This is the .NET domain name. Attempts to default from the underlying data project file path; uses the second to last segment of the child-most sub-directory by convention. For example, '/xxx/yyy/My.App.Sales.Database', the domain would be 'Sales'.")]
    public string? Domain { get; set; }

    /// <summary>
    /// Indicates the default entity-framework code-generation choice.
    /// </summary>
    [JsonPropertyName("efModel")]
    [CodeGenProperty("Entity Framework", Title = "The default entity-framework code-generation choice.", Description = "Defaults to 'Yes' (indicates combination of 'ModelOnly' and 'ModelBuilderOnly').", Options = ["Yes", "No", "ModelOnly", "ModelBuilderOnly"])]
    public string? EfModel { get; set; }

    /// <summary>
    /// Gets or sets the relative path to the data-related .NET project.
    /// </summary>
    [JsonPropertyName("dotNetDataProjectPath")]
    [CodeGenProperty("Paths", Title = "The relative path for the .NET data-related project.", Description = "Defaults to automatic inference using expected name of 'Infrastructure'.")]
    public string? DotNetDataProjectPath { get; set; }

    /// <summary>
    /// Gets or sets the path to append to the <see cref="DotNetDataProjectPath"/> for the .NET generated entity-framework repository code.
    /// </summary>
    [JsonPropertyName("dotNetDataEfRepositoriesPath")]
    [CodeGenProperty("Paths", Title = "The path to append to the '{DotNetDataProjectPath}' for the .NET generated entity-framework repository code.", Description = "Defaults to 'Repositories'.")]
    public string? DotNetDataEfRepositoriesPath { get; set; }

    /// <summary>
    /// Gets or sets the path to append to the <see cref="DotNetDataProjectPath"/> for the .NET generated entity-framework models code.
    /// </summary>
    [JsonPropertyName("dotNetDataEfModelsPath")]
    [CodeGenProperty("Paths", Title = "The path to append to the '{DotNetDataProjectPath}' for the .NET generated entity-framework models code.", Description = "Defaults to 'Persistence'.")]
    public string? DotNetDataEfModelsPath { get; set; }

    /// <summary>
    /// Gets or sets the .NET namespace for the entity-framework generated repository code.
    /// </summary>
    public string? DotNetDataEfRepositoriesNamespace { get; set; }

    /// <summary>
    /// Gets or sets the .NET namespace for the entity-framework generated persistence models code.
    /// </summary>
    public string? DotNetDataEfModelsNamespace { get; set; }

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

    #endregion

    #region Outbox

    /// <summary>
    /// Indicates whether to generate the transactional-outbox database capabilities.
    /// </summary>
    [JsonPropertyName("outbox")]
    [CodeGenProperty("Outbox", Title = "Indicates whether to generate the transactional-outbox database capabilities.", Description = "Defaults to 'false'.")]
    public bool? Outbox { get; set; }

    /// <summary>
    /// Gets or sets the database schema name used for outbox tables and stored procedures.
    /// </summary>
    [JsonPropertyName("outboxSchema")]
    [CodeGenProperty("Outbox", Title = "The database schema name for the outbox tables and stored procedures.", Description = "Defaults to '{schema}'.")]
    public string? OutboxSchema { get; set; }

    /// <summary>
    /// Gets or sets the name of the outbox table used for storing outgoing messages.
    /// </summary>
    [JsonPropertyName("outboxName")]
    [CodeGenProperty("Outbox", Title = "The name of the outbox table.", Description = "Defaults to 'Outbox'.")]
    public string? OutboxName { get; set; }

    #endregion

    /// <summary>
    /// Gets the database table collection configuration.
    /// </summary>
    [JsonPropertyName("tables")]
    [CodeGenPropertyCollection("Collections", Title = "The database table collection configuration.", IsImportant = true)]
    public List<TableConfig>? Tables { get; set; }

    /// <summary>
    /// Gets the database tables collection where the entity-framework model code-generation indicator is 'Yes' or 'ModelOnly'.
    /// </summary>
    public List<TableConfig> EfModels => Tables?.Where(x => x.EfModel == "Yes" || x.EfModel == "ModelOnly").ToList() ?? [];

    /// <summary>
    /// Gets the database tables collection where the entity-framework model code-generation indicator is 'Yes' or 'ModelBuilderOnly'.
    /// </summary>
    public List<TableConfig> EfModelBuilders => Tables?.Where(x => x.EfModel == "Yes" || x.EfModel == "ModelBuilderOnly").ToList() ?? [];

    /// <summary>
    /// Gets the <see cref="DirectoryInfo"/> for the initiating database project itself.
    /// </summary>
    public DirectoryInfo? DatabaseDirectory { get; private set; }

    /// <summary>
    /// Gets the <see cref="DirectoryInfo"/> for the <see cref="DotNetDataProjectPath"/>.
    /// </summary>
    public DirectoryInfo? DotNetDataProjectDirectory { get; private set; }

    /// <summary>
    /// Gets or sets the list of tables that exist within the database.
    /// </summary>
    public List<DbTableSchema> DbTables => _dbTables!;

    /// <inheritdoc/>
    protected async override Task PrepareAsync()
    {
        // Get the migrator from the runtime parameters.
        _migrator = (RuntimeParameters.TryGetValue("__migrator", out var migrator) && migrator is DatabaseMigrationBase m ? m : null) ?? throw new CodeGenException("The 'Migrator' runtime parameter is not set.");

        // Get the base paths, etc.
        DatabaseDirectory = CodeGenArgs?.OutputDirectory ?? throw new InvalidOperationException("The 'OutputDirectory' property of the 'CodeGenArgs' is not set.");
        var parts = DatabaseDirectory.Name.Split('.');

        // Using the DotNet data project relative path, get the absolute path and ensure it exists.
        if (DotNetDataProjectPath is not null)
        { 
            if (!DotNetDataProjectPath.StartsWith('.'))
                throw new CodeGenException(this, nameof(DotNetDataProjectPath), $"'{DotNetDataProjectPath}' should be a relative path to '{CodeGenArgs.OutputDirectory.FullName}'.");
        }
        else
            DotNetDataProjectPath = Path.Combine("..", string.Join('.', parts.Take(parts.Length - 1).Append("Infrastructure")));

        DotNetDataProjectDirectory = new DirectoryInfo(Path.Combine(CodeGenArgs.OutputDirectory.FullName, DotNetDataProjectPath));
        if (!DotNetDataProjectDirectory.Exists)
            throw new CodeGenException(this, nameof(DotNetDataProjectPath), $"'{DotNetDataProjectPath}' does not exist relative to '{CodeGenArgs.OutputDirectory.FullName}'.");

        // Handle the EF paths and namespaces.
        DotNetDataEfRepositoriesPath = DefaultWhereNull(DotNetDataEfRepositoriesPath, () => "Repositories");
        DotNetDataEfModelsPath = DefaultWhereNull(DotNetDataEfModelsPath, () => "Persistence");

        DotNetDataEfRepositoriesNamespace = $"{DotNetDataProjectDirectory.Name}.{DotNetDataEfRepositoriesPath}";
        DotNetDataEfModelsNamespace = $"{DotNetDataProjectDirectory.Name}.{DotNetDataEfModelsPath}";

        // Default the domain name from the file path (2nd to last part) if not explicitly set.
        Domain = DefaultWhereNull(Domain, () => parts.Length >= 2 ? parts[^2] : null) ?? throw new CodeGenException(this, nameof(Domain), $"Could not be defaulted from the file path; please explicitly set the property in the configuration.");
        Schema = DefaultWhereNull(Schema, () => Migrator.SchemaConfig.SupportsSchema ? Domain : null);
        EfModel = DefaultWhereNull(EfModel, () => "Yes");

        // Default the by-convention properties.
        ColumnNameIsDeleted = DefaultWhereNull(ColumnNameIsDeleted, () => Migrator.SchemaConfig.IsDeletedColumnName ?? "IsDeleted");
        ColumnNameTenantId = DefaultWhereNull(ColumnNameTenantId, () => Migrator.SchemaConfig.TenantIdColumnName ?? "TenantId");
        ColumnNameRowVersion = DefaultWhereNull(ColumnNameRowVersion, () => Migrator.SchemaConfig.RowVersionColumnName ?? "RowVersion");
        ColumnNameCreatedBy = DefaultWhereNull(ColumnNameCreatedBy, () => Migrator.SchemaConfig.CreatedByColumnName ?? "CreatedBy");
        ColumnNameCreatedOn = DefaultWhereNull(ColumnNameCreatedOn, () => Migrator.SchemaConfig.CreatedOnColumnName ?? "CreatedOn");
        ColumnNameUpdatedBy = DefaultWhereNull(ColumnNameUpdatedBy, () => Migrator.SchemaConfig.UpdatedByColumnName ?? "UpdatedBy");
        ColumnNameUpdatedOn = DefaultWhereNull(ColumnNameUpdatedOn, () => Migrator.SchemaConfig.UpdatedOnColumnName ?? "UpdatedOn");

        // Default the outbox properties.
        OutboxSchema = DefaultWhereNull(OutboxSchema, () => Schema);
        OutboxName = DefaultWhereNull(OutboxName, () => "Outbox");

        // Load the database tables and columns configuration.
        await LoadDbTablesConfigAsync().ConfigureAwait(false);

        Tables = await PrepareCollectionAsync(Tables).ConfigureAwait(false);
    }

    /// <summary>
    /// Load the database table and columns configuration.
    /// </summary>
    private async Task LoadDbTablesConfigAsync()
    {
        CodeGenArgs!.Logger?.Log(LogLevel.Information, "{Content}", string.Empty);
        CodeGenArgs.Logger?.Log(LogLevel.Information, "{Content}", $"Querying database to infer table(s)/column(s) schema configuration...");

        var sw = Stopwatch.StartNew();
        _dbTables = await Migrator.Database.SelectSchemaAsync(Migrator).ConfigureAwait(false);

        sw.Stop();
        CodeGenArgs.Logger?.Log(LogLevel.Information, "{Content}", $"  Database schema query complete [{sw.ElapsedMilliseconds}ms]");
    }
}