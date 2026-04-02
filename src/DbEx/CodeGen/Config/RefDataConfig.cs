namespace DbEx.CodeGen.Config;

/// <summary>
/// Provides the reference-data configuration as as extension of the <see cref="TableConfig"/>.
/// </summary>
public class RefDataConfig() : ConfigBase<CodeGenConfig, TableConfig>
{
    /// <summary>
    /// Gets the column configuration for the property named "Code", if it exists.
    /// </summary>
    public ColumnConfig? CodeProperty => Parent!.Columns!.SingleOrDefault(c => c.Property == "Code");

    /// <summary>
    /// Gets the column configuration for the property named "Text", if it exists.
    /// </summary>
    public ColumnConfig? TextProperty => Parent!.Columns!.SingleOrDefault(c => c.Property == "Text");

    /// <summary>
    /// Gets the column configuration for the property named "Description", if it exists.
    /// </summary>
    public ColumnConfig? DescriptionProperty => Parent!.Columns!.SingleOrDefault(c => c.Property == "Description");

    /// <summary>
    /// Gets the column configuration for the property named "SortOrder", if it exists.
    /// </summary>
    public ColumnConfig? SortOrderProperty => Parent!.Columns!.SingleOrDefault(c => c.Property == "SortOrder");

    /// <summary>
    /// Gets the column configuration for the property named "IsActive", if it exists.
    /// </summary>
    public ColumnConfig? IsActiveProperty => Parent!.Columns!.SingleOrDefault(c => c.Property == "IsActive");

    /// <summary>
    /// Gets the column configuration for the property named "StartsOn", if it exists.
    /// </summary>
    public ColumnConfig? StartsOnProperty => Parent!.Columns!.SingleOrDefault(c => c.Property == "StartsOn");

    /// <summary>
    /// Gets the column configuration for the property named "EndsOn", if it exists.
    /// </summary>
    public ColumnConfig? EndsOnProperty => Parent!.Columns!.SingleOrDefault(c => c.Property == "EndsOn");

    /// <summary>
    /// Gets the collection of reserved column configurations that are part of the standard set.
    /// </summary>
    public List<ColumnConfig> ReservedProperties => [.. Parent!.Columns!.Where(c => c.Property == "Code" || c.Property == "Text" || c.Property == "Description" || c.Property == "SortOrder" || c.Property == "IsActive" || c.Property == "StartsOn" || c.Property == "EndsOn")];

    /// <summary>
    /// Gets the collection of additional column configurations that are not part of the standard set of properties.
    /// </summary>
    public List<ColumnConfig> AdditionalProperties => [.. Parent!.StandardColumns!.Where(c => c.Property != "Code" && c.Property != "Text" && c.Property != "Description" && c.Property != "SortOrder" && c.Property != "IsActive" && c.Property != "StartsOn" && c.Property != "EndsOn")];

    /// <inheritdoc/>
    protected override Task PrepareAsync() => Task.CompletedTask;
}