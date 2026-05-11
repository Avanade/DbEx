namespace DbEx.CodeGen.Config;

/// <summary>
/// Provides the standardized set of special column names.
/// </summary>
public interface IByConventionColumnNames
{
    /// <summary>
    /// Gets or sets the column name for the 'IsDeleted' capability.
    /// </summary>
    string? ColumnNameIsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the column name for the 'TenantId' capability.
    /// </summary>
    string? ColumnNameTenantId { get; set; }

    /// <summary>
    /// Gets or sets the column name for the 'RowVersion' capability.
    /// </summary>
    string? ColumnNameRowVersion { get; set; }

    /// <summary>
    /// Gets or sets the column name for the 'CreatedBy' capability.
    /// </summary>
    string? ColumnNameCreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the column name for the 'CreatedOn' capability.
    /// </summary>
    string? ColumnNameCreatedOn { get; set; }

    /// <summary>
    /// Gets or sets the column name for the 'UpdatedBy' capability.
    /// </summary>
    string? ColumnNameUpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets the column name for the 'UpdatedOn' capability.
    /// </summary>
    string? ColumnNameUpdatedOn { get; set; }
}