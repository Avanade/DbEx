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
        /// Indicates whether the foreign key is references a reference data table/entity.
        /// </summary>
        public bool IsForeignRefData { get; set; }

        /// <summary>
        /// Gets or sets the foreign key reference data column name.
        /// </summary>
        public string? ForeignRefDataCodeColumn { get; set; }

        /// <summary>
        /// Gets the corresponding .NET <see cref="System.Type"/> name.
        /// </summary>
        public string DotNetType => _dotNetType ?? throw new InvalidOperationException($"The {nameof(Prepare)} must be invoked before the {nameof(DotNetType)} property can be accessed.");

        /// <summary>
        /// Gets the fully defined SQL type.
        /// </summary>
        public string SqlType => _sqlType ?? throw new InvalidOperationException($"The {nameof(Prepare)} must be invoked before the {nameof(DotNetType)} property can be accessed.");

        /// <summary>
        /// Prepares the schema by updating the calcuated properties: <see cref="DotNetType"/> and <see cref="SqlType"/>.
        /// </summary>
        public void Prepare()
        {
            _dotNetType = DbTable.Config.GetDotNetTypeName(Type);
            _sqlType = DbTable.Config.GetFormattedSqlType(this);
        }

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
            ForeignRefDataCodeColumn = column.ForeignRefDataCodeColumn;
            Prepare();
        }
    }
}