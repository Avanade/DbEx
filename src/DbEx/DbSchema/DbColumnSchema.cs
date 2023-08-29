﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;
using System.Diagnostics;

namespace DbEx.DbSchema
{
    /// <summary>
    /// Represents the Database <b>Column</b> schema definition.
    /// </summary>
    [DebuggerDisplay("{Name} {SqlType} ({DotNetType})")]
    public class DbColumnSchema
    {
        private string? _dotNetType;
        private string? _dotNetName;
        private string? _sqlType;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbColumnSchema"/> class.
        /// </summary>
        /// <param name="dbTable">The owning (parent) <see cref="DbTableSchema"/>.</param>
        /// <param name="name">The column name.</param>
        /// <param name="type">The column type.</param>
        public DbColumnSchema(DbTableSchema dbTable, string name, string type)
        {
            DbTable = dbTable ?? throw new ArgumentNullException(nameof(dbTable));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        /// <summary>
        /// Gets the owning (parent) <see cref="DbTable"/>.
        /// </summary>
        public DbTableSchema DbTable { get; }

        /// <summary>
        /// Gets the column name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the SQL Server data type.
        /// </summary>
        public string Type { get; }

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
        /// Indicates whether the column <i>may</i> contain JSON content by convention (<see cref="DotNetType"/> is a `<c>string</c>` and the <see cref="Name"/> ends with `<c>Json</c>`) .
        /// </summary>
        public bool IsJsonContent => DotNetType == "string" && Name.EndsWith("Json", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the corresponding .NET <see cref="System.Type"/> name.
        /// </summary>
        public string DotNetType => _dotNetType ??= DbTable?.Config.ToDotNetTypeName(this) ?? throw new InvalidOperationException($"The {nameof(DbTable)} must be set before the {nameof(DotNetType)} property can be accessed.");

        /// <summary>
        /// Gets the corresponding .NET name.
        /// </summary>
        public string DotNetName => _dotNetName ??= DbTableSchema.CreateDotNetName(Name, IsRefData || IsJsonContent);

        /// <summary>
        /// Gets the fully defined SQL type.
        /// </summary>
        public string SqlType => _sqlType ??= DbTable?.Config.ToFormattedSqlType(this) ?? throw new InvalidOperationException($"The {nameof(DbTable)} must be set before the {nameof(SqlType)} property can be accessed.");

        /// <summary>
        /// Clones the <see cref="DbColumnSchema"/> creating a new instance.
        /// </summary>
        /// <returns></returns>
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
            IsNullable = (column ?? throw new ArgumentNullException(nameof(column))).IsNullable;
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
            _dotNetType = column._dotNetType;
            _dotNetName = column._dotNetName;
            _sqlType = column._sqlType;
        }
    }
}