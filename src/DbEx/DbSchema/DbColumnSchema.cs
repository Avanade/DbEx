// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx;
using System;
using System.Diagnostics;

namespace DbEx.DbSchema
{
    /// <summary>
    /// Represents the Database <b>Column</b> schema definition.
    /// </summary>
    /// <param name="dbTable">The owning (parent) <see cref="DbTableSchema"/>.</param>
    /// <param name="name">The column name.</param>
    /// <param name="type">The column type.</param>
    /// <param name="dotNetNameOverride">The .NET name override (optional).</param>
    [DebuggerDisplay("{Name} {SqlType} ({DotNetType})")]
    public class DbColumnSchema(DbTableSchema dbTable, string name, string type, string? dotNetNameOverride = null)
    {
        private string? _dotNetType;
        private string? _dotNetName = dotNetNameOverride;
        private string? _dotNetCleanedName;
        private string? _sqlType;
        private string? _sqlType2;

        /// <summary>
        /// Gets the owning (parent) <see cref="DbTable"/>.
        /// </summary>
        public DbTableSchema DbTable { get; } = dbTable.ThrowIfNull(nameof(dbTable));

        /// <summary>
        /// Gets the column name.
        /// </summary>
        public string Name { get; } = name.ThrowIfNull(nameof(name));

        /// <summary>
        /// Gets the SQL Server data type.
        /// </summary>
        public string Type { get; } = type.ThrowIfNull(nameof(type));

        /// <summary>
        /// Indicates whether the column is nullable.
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Gets or sets the length.
        /// </summary>
        public ulong? Length { get; set; }

        /// <summary>
        /// Indicates whether the column has a length greater than zero.
        /// </summary>
        public bool HasLength => Length != null && Length > 0;

        /// <summary>
        /// Gets or sets the precision.
        /// </summary>
        public ulong? Precision { get; set; }

        /// <summary>
        /// Gets or sets the scale.
        /// </summary>
        public ulong? Scale { get; set; }

        /// <summary>
        /// Indicates whether the column is an auto-incremented identity (either Identity or Defaulted).
        /// </summary>
        public bool IsIdentity { get; set; }

        /// <summary>
        /// Indicates whether the column is an auto-incremented seeded identity.
        /// </summary>
        public bool IsIdentitySeeded => IsIdentity && IdentitySeed != null;

        /// <summary>
        /// Gets or sets the identity seed value.
        /// </summary>
        public int? IdentitySeed { get; set; }

        /// <summary>
        /// Gets or sets the identity increment value;
        /// </summary>
        public int? IdentityIncrement { get; set; }

        /// <summary>
        /// Indicates whether the column is computed.
        /// </summary>
        public bool IsComputed { get; set; }

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        public string? DefaultValue { get; set; }

        /// <summary>
        /// Indicates whether the column is the primary key.
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// Indicates whether the column has a unique constraint.
        /// </summary>
        public bool IsUnique { get; set; }

        /// <summary>
        /// Gets or sets the foreign key table.
        /// </summary>
        public string? ForeignTable { get; set; }

        /// <summary>
        /// Gets or sets the foreign key schema.
        /// </summary>
        public string? ForeignSchema { get; set; }

        /// <summary>
        /// Gets or sets the foreign key column name.
        /// </summary>
        public string? ForeignColumn { get; set; }

        /// <summary>
        /// Indicates whether the column <see cref="IsForeignRefData"/> or the name (after removing '<c>Id</c>' or '<c>Code</c>') matches a reference data table/entity in the same schema (where applicable).
        /// </summary>
        public bool IsRefData { get; set; }

        /// <summary>
        /// Indicates whether the foreign key is referencing a reference data table/entity.
        /// </summary>
        public bool IsForeignRefData { get; set; }

        /// <summary>
        /// Gets or sets the foreign key reference data column name.
        /// </summary>
        public string? ForeignRefDataCodeColumn { get; set; }

        /// <summary>
        /// Indicates whether the column is a created audit column; i.e. name is <c>CreatedDate</c> or <c>CreatedBy</c>.
        /// </summary>
        public bool IsCreatedAudit { get; set; }

        /// <summary>
        /// Indicates whether the column is an updated audit column; i.e. name is <c>UpdatedDate</c> or <c>UpdatedBy</c>.
        /// </summary>
        public bool IsUpdatedAudit { get; set; }

        /// <summary>
        /// Indicates whether the column is a row-version column; i.e. name is <c>RowVersion</c>.
        /// </summary>
        public bool IsRowVersion { get; set; }

        /// <summary>
        /// Indicates whether the column is a tenant identifier column; i.e. name is <c>TenantId</c>.
        /// </summary>
        public bool IsTenantId { get; set; }

        /// <summary>
        /// Indicates whether the column is an is-deleted column; i.e. name is <c>IsDeleted</c>.
        /// </summary>
        public bool IsIsDeleted { get; set; }

        /// <summary>
        /// Indicates whether the column <i>may</i> contain JSON content by convention (<see cref="DotNetType"/> is a `<c>string</c>` and the <see cref="Name"/> ends with `<c>Json</c>` or is a native JSON database type).
        /// </summary>
        public bool IsJsonContent { get; set; }

        /// <summary>
        /// Gets the corresponding .NET <see cref="System.Type"/> name.
        /// </summary>
        public string DotNetType => _dotNetType ??= DbTable?.Migration.SchemaConfig.ToDotNetTypeName(this) ?? throw new InvalidOperationException($"The {nameof(DbTable)} must be set before the {nameof(DotNetType)} property can be accessed.");

        /// <summary>
        /// Gets the corresponding .NET name.
        /// </summary>
        public string DotNetName => _dotNetName ??= DbTableSchema.CreateDotNetName(Name);

        /// <summary>
        /// Gets the corresponding .NET name cleaned; by removing any known suffixes where <see cref="IsRefData"/> or <see cref="IsJsonContent"/> 
        /// </summary>
        public string DotNetCleanedName { get => _dotNetCleanedName ?? DotNetName; set => _dotNetCleanedName = value; }

        /// <summary>
        /// Gets the fully defined SQL type (includes nullability).
        /// </summary>
        public string SqlType => _sqlType ??= DbTable?.Migration.SchemaConfig.ToFormattedSqlType(this) ?? throw new InvalidOperationException($"The {nameof(DbTable)} must be set before the {nameof(SqlType)} property can be accessed.");

        /// <summary>
        /// Gets the fully defined SQL type (excludes nullability).
        /// </summary>
        public string SqlType2 => _sqlType2 ??= DbTable?.Migration.SchemaConfig.ToFormattedSqlType(this, false) ?? throw new InvalidOperationException($"The {nameof(DbTable)} must be set before the {nameof(SqlType)} property can be accessed.");

#if NET7_0_OR_GREATER
        /// <summary>
        /// Indicates that the type can be expressed as a <see cref="DateOnly"/> .NET type.
        /// </summary>
#else
        /// <summary>
        /// Indicates that the type can be expressed as a <c>DateOnly</c> .NET type.
        /// </summary>
#endif
        public bool IsDotNetDateOnly { get; set; }

#if NET7_0_OR_GREATER
        /// <summary>
        /// Indicates that the type can be expressed as a <see cref="TimeOnly"/> .NET type.
        /// </summary>
#else
        /// <summary>
        /// Indicates that the type can be expressed as a <c>TimeOnly</c> .NET type.
        /// </summary>
#endif
        public bool IsDotNetTimeOnly { get; set; }

        /// <summary>
        /// Clones the <see cref="DbColumnSchema"/> creating a new instance.
        /// </summary>
        public DbColumnSchema Clone()
        {
            var c = new DbColumnSchema(DbTable, Name, Type);
            c.CopyFrom(this);
            return c;
        }

        /// <summary>
        /// Copy all properties (excluding <see cref="DbTable"/>, <see cref="Name"/> and <see cref="Type"/>) from specified <paramref name="column"/>.
        /// </summary>
        /// <param name="column">The <see cref="DbColumnSchema"/> to copy from.</param>
        public void CopyFrom(DbColumnSchema column)
        {
            _dotNetType = column.ThrowIfNull(nameof(column))._dotNetType;
            _dotNetName = column._dotNetName;
            _dotNetCleanedName = column._dotNetCleanedName;
            _sqlType = column._sqlType;
            IsNullable = column.IsNullable;
            Length = column.Length;
            Precision = column.Precision;
            Scale = column.Scale;
            IsIdentity = column.IsIdentity;
            IdentityIncrement = column.IdentityIncrement;
            IdentitySeed = column.IdentitySeed;
            IsComputed = column.IsComputed;
            DefaultValue = column.DefaultValue;
            IsPrimaryKey = column.IsPrimaryKey;
            IsUnique = column.IsUnique;
            ForeignTable = column.ForeignTable;
            ForeignSchema = column.ForeignSchema;
            ForeignColumn = column.ForeignColumn;
            IsForeignRefData = column.IsForeignRefData;
            IsRefData = column.IsRefData;
            ForeignRefDataCodeColumn = column.ForeignRefDataCodeColumn;
            IsCreatedAudit = column.IsCreatedAudit;
            IsUpdatedAudit = column.IsUpdatedAudit;
            IsRowVersion = column.IsRowVersion;
            IsTenantId = column.IsTenantId;
            IsIsDeleted = column.IsIsDeleted;
            IsDotNetDateOnly = column.IsDotNetDateOnly;
            IsDotNetTimeOnly = column.IsDotNetTimeOnly;
        }
    }
}