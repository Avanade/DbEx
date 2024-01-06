// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;
using System.Collections;
using System.Collections.Generic;

namespace DbEx.Migration.Data
{
    /// <summary>
    /// Provides <see cref="DataParser"/> schema, table and column name mappings.
    /// </summary>
    public class DataParserTableNameMappings : IEnumerable<KeyValuePair<(string ParsedSchema, string ParsedTable), (string Schema, string Table, Dictionary<string, string>? ColumnMappings)>>
    {
        private readonly Dictionary<(string, string), (string, string, Dictionary<string, string>?)> _dict = [];

        /// <summary>
        /// Adds a schema, table and column(s) mapping.
        /// </summary>
        /// <param name="parsedSchema">The parsed schema name.</param>
        /// <param name="parsedTable">The parsed table name.</param>
        /// <param name="schema">The mapped database schema name.</param>
        /// <param name="table">The mapped database table name.</param>
        /// <param name="columnMappings">The optional parsed and mapped database column names.</param>
        /// <returns>The <see cref="DataParserTableNameMappings"/> instance to support fluent-style method-chaining.</returns>
        public DataParserTableNameMappings Add(string? parsedSchema, string parsedTable, string? schema, string table, Dictionary<string, string>? columnMappings = null)
        {
            _dict.Add((EmptyWhereNull(parsedSchema), parsedTable), (EmptyWhereNull(schema), table, columnMappings));
            return this;
        }

        /// <summary>
        /// Adds column(s) mappings.
        /// </summary>
        /// <param name="schema">The mapped database schema name.</param>
        /// <param name="table">The mapped database table name.</param>
        /// <param name="columnMappings">The parsed and mapped database column names.</param>
        /// <returns>The <see cref="DataParserTableNameMappings"/> instance to support fluent-style method-chaining.</returns>
        public DataParserTableNameMappings Add(string? schema, string table, Dictionary<string, string> columnMappings)
        {
            _dict.Add((EmptyWhereNull(schema), table), (EmptyWhereNull(schema), table, columnMappings ?? throw new ArgumentNullException(nameof(columnMappings))));
            return this;
        }

        /// <summary>
        /// Gets the table mapping.
        /// </summary>
        /// <param name="parsedSchema">The parsed schema name.</param>
        /// <param name="parsedTable">The parsed table name.</param>
        /// <returns>The mapped database schema, table and column name mappings.</returns>
        public (string Schema, string Table, IDictionary<string, string>? ColumnMappings) Get(string? parsedSchema, string parsedTable)
            => _dict.TryGetValue((EmptyWhereNull(parsedSchema), parsedTable), out var value) ? value : new (EmptyWhereNull(parsedSchema), parsedTable, null);

        /// <summary>
        /// Empties the value where null.
        /// </summary>
        private static string EmptyWhereNull(string? value) => value ?? string.Empty;

        /// <summary>
        /// Removes all mappings.
        /// </summary>
        public void Clear() => _dict.Clear();

        /// <inheritdoc/>
        IEnumerator<KeyValuePair<(string ParsedSchema, string ParsedTable), (string Schema, string Table, Dictionary<string, string>? ColumnMappings)>> IEnumerable<KeyValuePair<(string ParsedSchema, string ParsedTable), (string Schema, string Table, Dictionary<string, string>? ColumnMappings)>>.GetEnumerator()
            => _dict.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => _dict.GetEnumerator();
    }
}