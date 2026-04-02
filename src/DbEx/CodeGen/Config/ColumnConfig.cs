namespace DbEx.CodeGen.Config;

/// <summary>
/// Provides the database column code-generation configuration.
/// </summary>
[CodeGenClass("Column", Title = "Database column configuration.")]
[CodeGenCategory("Primary", Title = "Provides the _primary_ configuration.")]
[CodeGenCategory("Entity Framework", Title = "Provides the configuration for the Entity Framework (EF) capabilities.")]
public class ColumnConfig : ConfigBase<CodeGenConfig, TableConfig>
{
    /// <summary>
    /// Gets or sets the database column name.
    /// </summary>
    [JsonPropertyName("name")]
    [CodeGenProperty("Primary", Title = "The database column name.", IsMandatory = true)]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the .NET equivalent name for the column.
    /// </summary>
    [JsonPropertyName("property")]
    [CodeGenProperty("Entity Framework", Title = "The .NET property name equivalent for the column.", IsImportant = true, Description = "Defaults to the database column's .NET formatted name.")]
    public string? Property { get; set; }

    /// <summary>
    /// Gets or sets the corresponding .NET type for the column.
    /// </summary>
    [JsonPropertyName("type")]
    [CodeGenProperty("Entity Framework", Title = "The corresponding .NET type equivalent for the column (including nullability).", IsImportant = true, Description = "Defaults to the database column's .NET type.")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the .NET value converter source code for the column (where applicable).
    /// </summary>
    [JsonPropertyName("valueConverter")]
    [CodeGenProperty("Entity Framework", Title = "The .NET value converter source code for the column.", Description = "Defaults to null. This must be valid C# source code as it is applied as-is.")]
    public string? ValueConverter { get; set; }

    /// <summary>
    /// Gets or sets the actual <see cref="DbColumnSchema"/>.
    /// </summary>
    public DbColumnSchema? DbColumn { get; set; }

    /// <inheritdoc/>
    protected override Task PrepareAsync()
    {
        DbColumn ??= Parent?.DbTable?.Columns.SingleOrDefault(x => x.Name == Name) ?? throw new CodeGenException(this, nameof(Name), $"Column '{Name}' for table '{Root!.Migrator.SchemaConfig.ToFullyQualifiedTableName(Parent!.Schema, Parent.Name!)}' not found in database.");
        Property = DefaultWhereNull(Property, () => DbColumn.DotNetName);
        Type = DefaultWhereNull(Type, () => DbColumn.DotNetTypeWithNullability);

        return Task.CompletedTask;
    }
}