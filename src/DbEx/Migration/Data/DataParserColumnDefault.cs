// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;

namespace DbEx.Migration.Data
{
    /// <summary>
    /// Provides the <see cref="DataParser"/> <see cref="DataParserArgs.ColumnDefaults"/> configuration.
    /// </summary>
    public class DataParserColumnDefault
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataParserColumnDefault"/> class.
        /// </summary>
        /// <param name="schema">The schema name; a '<c>*</c>' denotes any schema.</param>
        /// <param name="table">The table name; a '<c>*</c>' denotes any table.</param>
        /// <param name="column">The name of the column to be updated.</param>
        /// <param name="default">The function that provides the default value.</param>
        public DataParserColumnDefault(string schema, string table, string column, Func<int, object?> @default)
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            Table = table ?? throw new ArgumentNullException(nameof(table));
            Column = column ?? throw new ArgumentNullException(nameof(column));
            Default = @default ?? throw new ArgumentNullException(nameof(@default));
        }

        /// <summary>
        /// Gets the schema name; a '<c>*</c>' denotes any schema.
        /// </summary>
        public string Schema { get; }

        /// <summary>
        /// Gets the table name; a '<c>*</c>' denotes any table.
        /// </summary>
        public string Table { get; }

        /// <summary>
        /// Gets the column name.
        /// </summary>
        public string Column { get; }

        /// <summary>
        /// Gets the function that provides the default value.
        /// </summary>
        public Func<int, object?> Default { get; }
    }
}