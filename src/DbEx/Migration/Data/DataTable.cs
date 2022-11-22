// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.DbSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DbEx.Migration.Data
{
    /// <summary>
    /// Represents a database relational data table.
    /// </summary>
    public class DataTable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataTable"/> class.
        /// </summary>
        /// <param name="parser">The owning (parent) <see cref="Data.DataParser"/>.</param>
        /// <param name="schema">The schema name.</param>
        /// <param name="name">The table name.</param>
        internal DataTable(DataParser parser, string schema, string name)
        {
            Parser = parser ?? throw new ArgumentNullException(nameof(parser));

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (name.StartsWith('$'))
            {
                IsMerge = true;
                name = name[1..];
            }

            if (name.StartsWith('^'))
            {
                UseIdentifierGenerator = true;
                name = name[1..];
            }

            SchemaTableName = $"'{(schema == string.Empty ? name : $"{schema}.{name}")}'";

            DbTable = Parser.DbTables.Where(t => (!Parser.DatabaseSchemaConfig.SupportsSchema || t.Schema == schema) && t.Name == name).SingleOrDefault();
            if (DbTable == null)
                throw new DataParserException($"Table {SchemaTableName} does not exist within the specified database.");

            // Check that an identifier generator can be used.
            if (UseIdentifierGenerator)
            {
                if (DbTable.PrimaryKeyColumns.Count == 1 && Enum.TryParse<DataTableIdentifierType>(DbTable.PrimaryKeyColumns[0].DotNetType, true, out var igType))
                    IdentifierType = igType;
                else
                    throw new DataParserException($"Table {SchemaTableName} specifies usage of {nameof(IIdentifierGenerator)}; either there is more than one column representing the primary key or the underlying type is not supported.");
            }
        }

        /// <summary>
        /// Gets the owning (parent) <see cref="Data.DataParser"/>.
        /// </summary>
        public DataParser Parser { get; }

        /// <summary>
        /// Gets the <see cref="DataParserArgs"/>.
        /// </summary>
        public DataParserArgs Args => Parser.Args;

        /// <summary>
        /// Gets the schema name.
        /// </summary>
        public string Schema => DbTable.Schema;

        /// <summary>
        /// Gets the table name.
        /// </summary>
        public string Name => DbTable.Name;

        /// <summary>
        /// Gets the full formatted schema and table name.
        /// </summary>
        public string SchemaTableName { get; }

        /// <summary>
        /// Gets the underlying <see cref="DbTableSchema"/>.
        /// </summary>
        public DbTableSchema DbTable { get; }

        /// <summary>
        /// Indicates whether the table is reference data.
        /// </summary>
        public bool IsRefData => DbTable.IsRefData;

        /// <summary>
        /// Indicates whether the table data is to be merged.
        /// </summary>
        public bool IsMerge { get; }

        /// <summary>
        /// Indicates whether to use the identifier generator for the primary key (single column) on create (where not specified).
        /// </summary>
        public bool UseIdentifierGenerator { get; }

        /// <summary>
        /// Gets the identifier generator (see <see cref="UseIdentifierGenerator"/>) <see cref="Type"/>.
        /// </summary>
        public DataTableIdentifierType? IdentifierType { get; }

        /// <summary>
        /// Gets the columns.
        /// </summary>
        public List<DbColumnSchema> Columns { get; } = new List<DbColumnSchema>();

        /// <summary>
        /// Gets the merge match columns.
        /// </summary>
        public List<DbColumnSchema> MergeMatchColumns => Columns.Where(x => 
            !(x.Name == Args.CreatedDateColumnName || x.Name == Args.CreatedByColumnName || x.Name == Args.UpdatedDateColumnName || x.Name == Args.UpdatedDateColumnName)
            && !(UseIdentifierGenerator && x.IsPrimaryKey)).ToList();

        /// <summary>
        /// Gets the merge update columns.
        /// </summary>
        public List<DbColumnSchema> MergeUpdateColumns => Columns.Where(x => !(UseIdentifierGenerator && x.IsPrimaryKey)).ToList();

        /// <summary>
        /// Gets the primary key columns.
        /// </summary>
        public List<DbColumnSchema> PrimaryKeyColumns => Columns.Where(x => x.IsPrimaryKey).ToList();

        /// <summary>
        /// Gets the rows.
        /// </summary>
        public List<DataRow> Rows { get; } = new List<DataRow>();

        /// <summary>
        /// Adds a row (key value pairs of column name and corresponding value).
        /// </summary>
        /// <param name="row">The row.</param>
        public void AddRow(DataRow row)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            foreach (var c in row.Columns)
            {
                AddColumn(c.Name!);
            }

            Rows.Add(row);
        }

        /// <summary>
        /// Add to specified columns.
        /// </summary>
        private void AddColumn(string name)
        {
            var column = DbTable.Columns.Where(x => x.Name == name).SingleOrDefault();
            if (column == null)
                return;

            if (!Columns.Any(x => x.Name == name))
                Columns.Add(column);
        }

        /// <summary>
        /// Prepares the data.
        /// </summary>
        internal async Task PrepareAsync(CancellationToken cancellationToken)
        {
            for (int i = 0; i < Rows.Count; i++)
            {
                var row = Rows[i];
                await AddColumnWhereNotSpecifiedAsync(row, Args.CreatedDateColumnName ?? Parser.DatabaseSchemaConfig.CreatedDateColumnName, () => Task.FromResult<object?>(Args.DateTimeNow)).ConfigureAwait(false);
                await AddColumnWhereNotSpecifiedAsync(row, Args.CreatedByColumnName ?? Parser.DatabaseSchemaConfig.CreatedByColumnName, () => Task.FromResult<object?>(Args.UserName)).ConfigureAwait(false);
                await AddColumnWhereNotSpecifiedAsync(row, Args.UpdatedDateColumnName ?? Parser.DatabaseSchemaConfig.UpdatedDateColumnName, () => Task.FromResult<object?>(Args.DateTimeNow)).ConfigureAwait(false);
                await AddColumnWhereNotSpecifiedAsync(row, Args.UpdatedByColumnName ?? Parser.DatabaseSchemaConfig.UpdatedByColumnName, () => Task.FromResult<object?>(Args.UserName)).ConfigureAwait(false);

                if (IsRefData && Args.RefDataColumnDefaults != null)
                {
                    foreach (var rdd in Args.RefDataColumnDefaults)
                    {
                        await AddColumnWhereNotSpecifiedAsync(row, rdd.Key, () => Task.FromResult(rdd.Value(i + 1))).ConfigureAwait(false);
                    }
                }

                if (UseIdentifierGenerator)
                {
                    var pkc = DbTable.PrimaryKeyColumns[0];
                    var val = row.Columns.SingleOrDefault(x => x.Name == pkc.Name!);
                    if (val == null)
                    {
                        switch (IdentifierType)
                        {
                            case DataTableIdentifierType.Guid:
                                await AddColumnWhereNotSpecifiedAsync(row, pkc.Name!, async () => await Args.IdentifierGenerator.GenerateGuidIdentifierAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
                                break;

                            case DataTableIdentifierType.String:
                                await AddColumnWhereNotSpecifiedAsync(row, pkc.Name!, async () => await Args.IdentifierGenerator.GenerateStringIdentifierAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
                                break;

                            case DataTableIdentifierType.Int:
                                await AddColumnWhereNotSpecifiedAsync(row, pkc.Name!, async () => await Args.IdentifierGenerator.GenerateInt32IdentifierAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
                                break;

                            case DataTableIdentifierType.Long:
                                await AddColumnWhereNotSpecifiedAsync (row, pkc.Name!, async () => await Args.IdentifierGenerator.GenerateInt64IdentifierAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds the column where not already specified.
        /// </summary>
        private async Task AddColumnWhereNotSpecifiedAsync(DataRow row, string name, Func<Task<object?>> value)
        {
            if (DbTable.Columns.Any(x => x.Name == name) && !row.Columns.Any(x => x.Name == name))
            {
                AddColumn(name);
                row.AddColumn(name, await value().ConfigureAwait(false));
            }
        }
    }
}