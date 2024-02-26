// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx;
using System;

namespace DbEx.Migration.Data
{
    /// <summary>
    /// Provides the <see cref="DataParser"/> <see cref="DataParserArgs.ColumnDefaults"/> configuration.
    /// </summary>
    /// <param name="schema">The schema name; a '<c>*</c>' denotes any schema.</param>
    /// <param name="table">The table name; a '<c>*</c>' denotes any table.</param>
    /// <param name="column">The name of the column to be updated.</param>
    /// <param name="default">The function that provides the default value.</param>
    public class DataParserColumnDefault(string schema, string table, string column, Func<int, object?> @default)
    {
        /// <summary>
        /// Gets the schema name; a '<c>*</c>' denotes any schema.
        /// </summary>
        public string Schema { get; } = schema.ThrowIfNull(nameof(schema));

        /// <summary>
        /// Gets the table name; a '<c>*</c>' denotes any table.
        /// </summary>
        public string Table { get; } = table.ThrowIfNull(nameof(table));

        /// <summary>
        /// Gets the column name.
        /// </summary>
        public string Column { get; } = column.ThrowIfNull(nameof(column));

        /// <summary>
        /// Gets the function that provides the default value.
        /// </summary>
        public Func<int, object?> Default { get; } = @default.ThrowIfNull(nameof(@default));
    }
}