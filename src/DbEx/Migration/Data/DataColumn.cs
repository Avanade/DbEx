// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.Schema;
using System;

namespace DbEx.Migration.Data
{
    /// <summary>
    /// Represents the database relational data column.
    /// </summary>
    public class DataColumn
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataColumn"/> class.
        /// </summary>
        /// <param name="table">The owning (parent) <see cref="DataTable"/>.</param>
        /// <param name="name"></param>
        internal DataColumn(DataTable table, string name)
        {
            Table = table ?? throw new ArgumentNullException(nameof(table));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Gets the <see cref="DataTable"/>.
        /// </summary>
        public DataTable Table { get; }

        /// <summary>
        /// Gets or sets the column name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the column value.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Gets or sets the underlying <see cref="DbColumnSchema"/> configuration.
        /// </summary>
        public DbColumnSchema? DbColumn { get; set; }

        /// <summary>
        /// Gets the SQL formatted value.
        /// </summary>
        public string SqlValue => ToSqlValue();

        /// <summary>
        /// Indicates whether to use a foreign key query for the identifier.
        /// </summary>
        public bool UseForeignKeyQueryForId { get; set; }

        /// <summary>
        /// Gets the value formatted for use in a SQL statement.
        /// </summary>
        /// <returns>The value formatted for use in a SQL statement.</returns>
        public string ToSqlValue()
        {
            if (Value == null)
                return "NULL";
            else if (Value is string str)
                return $"'{str.Replace("'", "''", StringComparison.Ordinal)}'";
            else if (Value is bool b)
                return b ? "1" : "0";
            else if (Value is Guid)
                return $"'{Value}'";
            else if (Value is DateTime dt)
                return $"'{dt.ToString(Table.Parser.Args.DateTimeFormat, System.Globalization.CultureInfo.InvariantCulture)}'";
            else if (Value is DateTimeOffset dto)
                return $"'{dto.ToString(Table.Parser.Args.DateTimeFormat, System.Globalization.CultureInfo.InvariantCulture)}'";
            else
                return Value.ToString()!;
        }
    }
}