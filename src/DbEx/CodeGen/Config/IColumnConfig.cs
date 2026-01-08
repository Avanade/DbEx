namespace DbEx.CodeGen.Config;

/// <summary>
/// Defines the column configuration.
/// </summary>
public interface IColumnConfig
{
    /// <summary>
    /// Gets the column name.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets the database <see cref="DbColumnSchema"/> configuration.
    /// </summary>
    DbColumnSchema? DbColumn { get; }

    /// <summary>
    /// Gets the qualified name (includes the alias).
    /// </summary>
    public string QualifiedName { get; }

    /// <summary>
    /// Gets the parameter name.
    /// </summary>
    public string ParameterName { get; }

    /// <summary>
    /// Gets the SQL type.
    /// </summary>
    public string? SqlType { get; }

    /// <summary>
    /// Gets the parameter SQL definition.
    /// </summary>
    public string? ParameterSql { get; }

    /// <summary>
    /// Gets the UDT SQL definition.
    /// </summary>
    public string? UdtSql { get; }

    /// <summary>
    /// Gets the where equality clause.
    /// </summary>
    public string WhereEquals { get; }

    /// <summary>
    /// Gets the SQL for defining initial value for comparisons.
    /// </summary>
    public string SqlInitialValue { get; }

    /// <summary>
    /// Indicates where the column is the "TenantId" column.
    /// </summary>
    public bool IsTenantIdColumn { get; }

    /// <summary>
    /// Indicates where the column is the "OrgUnitId" column.
    /// </summary>
    public bool IsOrgUnitIdColumn { get; }

    /// <summary>
    /// Indicates where the column is the "RowVersion" column.
    /// </summary>
    public bool IsRowVersionColumn { get; }

    /// <summary>
    /// Indicates where the column is the "IsDeleted" column.
    /// </summary>
    public bool IsIsDeletedColumn { get; }

    /// <summary>
    /// Indicates whether the column is considered an audit column.
    /// </summary>
    public bool IsAudit { get; }

    /// <summary>
    /// Indicates whether the column is "CreatedBy" or "CreatedDate".
    /// </summary>
    public bool IsCreated { get; }

    /// <summary>
    /// Indicates whether the column is "CreatedBy".
    /// </summary>
    public bool IsCreatedBy { get; }

    /// <summary>
    /// Indicates whether the column is "CreatedDate".
    /// </summary>
    public bool IsCreatedDate { get; }

    /// <summary>
    /// Indicates whether the column is "UpdatedBy" or "UpdatedDate".
    /// </summary>
    public bool IsUpdated { get; }

    /// <summary>
    /// Indicates whether the column is "UpdatedBy".
    /// </summary>
    public bool IsUpdatedBy { get; }

    /// <summary>
    /// Indicates whether the column is "UpdatedDate".
    /// </summary>
    public bool IsUpdatedDate { get; }

    /// <summary>
    /// Indicates whether the column is "DeletedBy" or "DeletedDate".
    /// </summary>
    public bool IsDeleted { get; }

    /// <summary>
    /// Indicates whether the column is "DeletedBy".
    /// </summary>
    public bool IsDeletedBy { get; }

    /// <summary>
    /// Indicates whether the column is "DeletedDate".
    /// </summary>
    public bool IsDeletedDate { get; }

    /// <summary>
    /// Indicates where the column should be considered for a 'Create' operation.
    /// </summary>
    public bool IsCreateColumn { get; }

    /// <summary>
    /// Indicates where the column should be considered for a 'Update' operation.
    /// </summary>
    public bool IsUpdateColumn { get; }

    /// <summary>
    /// Indicates where the column should be considered for a 'Delete' operation.
    /// </summary>
    public bool IsDeleteColumn { get; }

    /// <summary>
    /// Gets the EF SQL Type.
    /// </summary>
    public string? EfSqlType { get; }

    /// <summary>
    /// Gets the corresponding .NET <see cref="System.Type"/> name.
    /// </summary>
    public string DotNetType { get; }

    /// <summary>
    /// Indicates whether the .NET property is nullable.
    /// </summary>
    public bool IsDotNetNullable { get; }

    /// <summary>
    /// Gets the name alias.
    /// </summary>
    public string? NameAlias { get; }

    /// <summary>
    /// Gets the qualified name with the alias (used in a select).
    /// </summary>
    public string QualifiedNameWithAlias { get; }
}