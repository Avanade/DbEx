namespace DbEx.CodeGen.Config;

/// <summary>
/// Provides the standardized set of special column names.
/// </summary>
public interface ISpecialColumnNames
{
    /// <summary>
    /// Gets or sets the column name for the `IsDeleted` capability.
    /// </summary>
    string? ColumnNameIsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the column name for the `TenantId` capability.
    /// </summary>
    string? ColumnNameTenantId { get; set; }

    /// <summary>
    /// Gets or sets the column name for the `OrgUnitId` capability.
    /// </summary>
    string? ColumnNameOrgUnitId { get; set; }

    /// <summary>
    /// Gets or sets the column name for the `RowVersion` capability.
    /// </summary>
    string? ColumnNameRowVersion { get; set; }

    /// <summary>
    /// Gets or sets the column name for the `CreatedBy` capability.
    /// </summary>
    string? ColumnNameCreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the column name for the `CreatedOn` capability.
    /// </summary>
    string? ColumnNameCreatedDate { get; set; }

    /// <summary>
    /// Gets or sets the column name for the `UpdatedBy` capability.
    /// </summary>
    string? ColumnNameUpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets the column name for the `UpdatedOn` capability.
    /// </summary>
    string? ColumnNameUpdatedDate { get; set; }

    /// <summary>
    /// Gets or sets the column name for the `DeletedBy` capability.
    /// </summary>
    string? ColumnNameDeletedBy { get; set; }

    /// <summary>
    /// Gets or sets the column name for the `DeletedDate` capability.
    /// </summary>
    string? ColumnNameDeletedDate { get; set; }
}

/// <summary>
/// Provides the standardized set of special columns.
/// </summary>
public interface ISpecialColumns
{
    /// <summary>
    /// Gets the related TenantId column.
    /// </summary>
    IColumnConfig? ColumnTenantId { get; }

    /// <summary>
    /// Gets the related OrgUnitId column.
    /// </summary>
    IColumnConfig? ColumnOrgUnitId { get; }

    /// <summary>
    /// Gets the related RowVersion column.
    /// </summary>
    IColumnConfig? ColumnRowVersion { get; }

    /// <summary>
    /// Gets the related IsDeleted column.
    /// </summary>
    IColumnConfig? ColumnIsDeleted { get; }

    /// <summary>
    /// Gets the related CreatedBy column.
    /// </summary>
    IColumnConfig? ColumnCreatedBy { get; }

    /// <summary>
    /// Gets the related CreatedOn column.
    /// </summary>
    IColumnConfig? ColumnCreatedOn { get; }

    /// <summary>
    /// Gets the related UpdatedBy column.
    /// </summary>
    IColumnConfig? ColumnUpdatedBy { get; }

    /// <summary>
    /// Gets the related UpdatedDate column.
    /// </summary>
    IColumnConfig? ColumnUpdatedOn { get; }
}