// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.Schema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DbEx.Migration.Data
{
    /// <summary>
    /// Represents the database relational data row.
    /// </summary>
    public class DataRow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataRow"/> class.
        /// </summary>
        /// <param name="table">The parent <see cref="DataTable"/>.</param>
        internal DataRow(DataTable table) => Table = table ?? throw new ArgumentNullException(nameof(table));

        /// <summary>
        /// Gets the <see cref="DataTable"/>.
        /// </summary>
        public DataTable Table { get; }

        /// <summary>
        /// Gets the columns.
        /// </summary>
        public List<DataColumn> Columns { get; } = new List<DataColumn>();

        /// <summary>
        /// Adds a <see cref="DataColumn"/> to the row using the specified name and value.
        /// </summary>
        /// <param name="name">The column name.</param>
        /// <param name="value">The column value.</param>
        public void AddColumn(string name, object? value) => AddColumn(new DataColumn(Table, name) { Value = value });

        /// <summary>
        /// Adds a <see cref="DataColumn"/> to the row.
        /// </summary>
        /// <param name="column">The <see cref="DataColumn"/>.</param>
        public void AddColumn(DataColumn column)
        {
            if (column == null)
                throw new ArgumentNullException(nameof(column));

            if (string.IsNullOrEmpty(column.Name))
                throw new ArgumentException("Column.Name must have a value.", nameof(column));

            var col = Table.DbTable.Columns.Where(c => c.Name == column.Name).SingleOrDefault();
            if (col == null)
            {
                // Check and see if it is a reference data id.
                col = Table.DbTable.Columns.Where(c => c.Name == column.Name + "Id").SingleOrDefault();
                if (col == null || !col.IsForeignRefData)
                    throw new DataParserException($"Table '{Table.Schema}.{Table.Name}' does not have a column named '{column.Name}' or '{column.Name}Id'; or was not identified as a foreign key to Reference Data.");

                column.Name += "Id";
            }

            if (Columns.Any(x => x.Name == column.Name))
                throw new DataParserException($"Table '{Table.Schema}.{Table.Name}' column '{column.Name}' has been specified more than once.");

            column.DbColumn = col;
            Columns.Add(column);

            if (column.Value == null)
                return;

            string? str = null;
            try
            {
                str = column.Value is DateTime time ? time.ToString(Table.Parser.Args.DateTimeFormat, System.Globalization.CultureInfo.InvariantCulture) : column.Value.ToString()!;

                switch (DbTypeMapper.GetDotNetTypeName(col.Type))
                {
                    case "string": column.Value = str; break;
                    case "decimal": column.Value = decimal.Parse(str, System.Globalization.CultureInfo.InvariantCulture); break;
                    case "DateTime": column.Value = DateTime.Parse(str, System.Globalization.CultureInfo.InvariantCulture); break;
                    case "bool": column.Value = bool.Parse(str); break;
                    case "DateTimeOffset": column.Value = DateTimeOffset.Parse(str, System.Globalization.CultureInfo.InvariantCulture); break;
                    case "double": column.Value = double.Parse(str, System.Globalization.CultureInfo.InvariantCulture); break;
                    case "short": column.Value = short.Parse(str, System.Globalization.CultureInfo.InvariantCulture); break;
                    case "byte": column.Value = byte.Parse(str, System.Globalization.CultureInfo.InvariantCulture); break;
                    case "float": column.Value = float.Parse(str, System.Globalization.CultureInfo.InvariantCulture); break;
                    case "TimeSpan": column.Value = TimeSpan.Parse(str, System.Globalization.CultureInfo.InvariantCulture); break;

                    case "int":
                        if (int.TryParse(str, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int i))
                            column.Value = i;
                        else if (col.IsForeignRefData)
                        {
                            column.Value = str;
                            column.UseForeignKeyQueryForId = true;
                        }
                        else
                            throw new DataParserException($"Table '{Table.Schema}.{Table.Name}' column '{column.Name}' value '{str}' is not of Type '{typeof(int).Name}' or column is not a reference data foreign key.");

                        break;

                    case "long":
                        if (long.TryParse(str, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out long l))
                            column.Value = l;
                        else if (col.IsForeignRefData)
                        {
                            column.Value = str;
                            column.UseForeignKeyQueryForId = true;
                        }
                        else
                            throw new DataParserException($"Table '{Table.Schema}.{Table.Name}' column '{column.Name}' value '{str}' is not of Type '{typeof(long).Name}' or column is not a reference data foreign key.");

                        break;

                    case "Guid":
                        if (int.TryParse(str, out int a))
                            column.Value = DataValueConverter.IntToGuid(a);
                        else
                        {
                            if (Guid.TryParse(str, out Guid g))
                                column.Value = g;
                            else if (col.IsForeignRefData)
                            {
                                column.Value = str;
                                column.UseForeignKeyQueryForId = true;
                            }
                            else
                                throw new DataParserException($"Table '{Table.Schema}.{Table.Name}' column '{column.Name}' value '{str}' is not of Type '{typeof(Guid).Name}' or column is not a reference data foreign key.");
                        }

                        break;

                    default:
                        throw new DataParserException($"Table '{Table.Schema}.{Table.Name}' column '{column.Name}' type '{col.Type}' is not supported.");
                }
            }
            catch (FormatException fex)
            {
                if (col.IsForeignRefData)
                {
                    column.Value = str;
                    column.UseForeignKeyQueryForId = true;
                }
                else
                    throw new DataParserException($"'{Table.Schema}.{Table.Name}' column '{column.Name}' type '{col.Type}' cannot parse value '{column.Value}': {fex.Message}");
            }
        }
    }
}