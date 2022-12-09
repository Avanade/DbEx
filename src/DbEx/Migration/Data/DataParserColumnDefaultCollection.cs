// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.DbSchema;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace DbEx.Migration.Data
{
    /// <summary>
    /// Provides a <see cref="DataParserColumnDefault"/> keyed collection.
    /// </summary>
    public class DataParserColumnDefaultCollection : KeyedCollection<(string, string, string), DataParserColumnDefault>
    {
        /// <inheritdoc/>
        protected override (string, string, string) GetKeyForItem(DataParserColumnDefault item) => (item.Schema, item.Table, item.Column);

        /// <summary>
        /// Attempts to get the <paramref name="item"/> for the specified <paramref name="schema"/>, <paramref name="table"/> and <paramref name="column"/> names.
        /// </summary>
        /// <param name="schema">The schema name.</param>
        /// <param name="table">The table name.</param>
        /// <param name="column">The column name.</param>
        /// <param name="item">The corresponding <see cref="DataParserColumnDefault"/> item where found; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> where found; otherwise, <c>false</c>.</returns>
        /// <remarks>Attempts to match as follows:
        ///  <list type="number">
        ///   <item>Schema, table and column names match item exactly;</item>
        ///   <item>Schema and column names match item exactly, and the underlying default table name is configured with '<c>*</c>';</item>
        ///   <item>Column names match item exactly, and the underlying default schema and table names are both configured with '<c>*</c>';</item>
        ///   <item>Item is not found.</item>
        ///  </list>
        /// </remarks>
        public bool TryGetValue(string schema, string table, string column, [NotNullWhen(true)] out DataParserColumnDefault? item)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            if (table == null)
                throw new ArgumentNullException(nameof(table));

            if (column == null)
                throw new ArgumentNullException(nameof(column));

            if (TryGetValue((schema, table, column), out item))
                return true;

            if (TryGetValue((schema, "*", column), out item))
                return true;

            if (TryGetValue(("*", "*", column), out item))
                return true;

            item = null;
            return false;
        }

        /// <summary>
        /// Get all the configured column defaults for the specified <paramref name="table"/>.
        /// </summary>
        /// <param name="table">The <see cref="DbTableSchema"/>.</param>
        /// <returns>The configured defaults.</returns>
        public DataParserColumnDefaultCollection GetDefaultsForTable(DbTableSchema table)
        {
            var dc = new DataParserColumnDefaultCollection();
            foreach (var c in (table ?? throw new ArgumentNullException(nameof(table))).Columns)
            {
                if (TryGetValue(table.Schema, table.Name, c.Name, out var item))
                    dc.Add(item);
            }

            return dc;
        }
    }
}