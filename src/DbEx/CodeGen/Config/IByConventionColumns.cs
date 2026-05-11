namespace DbEx.CodeGen.Config;

/// <summary>
/// Enables the by-convention columns configurations.
/// </summary>
public interface IByConventionColumns
{
    /// <summary>
    /// Gets or sets the column for the 'IsDeleted' capability.
    /// </summary>
    ColumnConfig? ColumnIsDeleted { get; }

    /// <summary>
    /// Gets or sets the column for the 'TenantId' capability.
    /// </summary>
    ColumnConfig? ColumnTenantId { get; }

    /// <summary>
    /// Gets or sets the column for the 'RowVersion' capability.
    /// </summary>
    ColumnConfig? ColumnRowVersion { get; }

    /// <summary>
    /// Gets or sets the column for the 'CreatedBy' capability.
    /// </summary>
    ColumnConfig? ColumnCreatedBy { get; }

    /// <summary>
    /// Gets or sets the column for the 'CreatedOn' capability.
    /// </summary>
    ColumnConfig? ColumnCreatedOn { get; }

    /// <summary>
    /// Gets or sets the column for the 'UpdatedBy' capability.
    /// </summary>
    ColumnConfig? ColumnUpdatedBy { get; }

    /// <summary>
    /// Gets or sets the column for the 'UpdatedOn' capability.
    /// </summary>
    ColumnConfig? ColumnUpdatedOn { get; }

    /// <summary>
    /// Indicates whether at least one of the audit columns (CreatedBy, CreatedOn, UpdatedBy, UpdatedOn) is present.
    /// </summary>
    bool HasAtLeastOneAuditColumn { get; }
}