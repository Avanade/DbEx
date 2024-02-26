// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx;
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
            Parser = parser.ThrowIfNull(nameof(parser));
            name.ThrowIfNull(nameof(name));

            // Determine features by notation/convention.
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

            // Determine the schema, table and column name mappings.
            var mappings = parser.ParserArgs.TableNameMappings.Get(schema, name);
            schema = mappings.Schema;
            name = mappings.Table;
            ColumnNameMappings = mappings.ColumnMappings ?? new Dictionary<string, string>();

            // Get the database table.
            SchemaTableName = $"'{(schema == string.Empty ? name : $"{schema}.{name}")}'";
            DbTable = Parser.DbTables.Where(t => (!Parser.Migration.SchemaConfig.SupportsSchema || t.Schema == schema) && t.Name == name).SingleOrDefault() ??
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
        public DataParserArgs ParserArgs => Parser.ParserArgs;

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
        public List<DbColumnSchema> Columns { get; } = [];

        /// <summary>
        /// Gets the insert columns.
        /// </summary>
        public List<DbColumnSchema> InsertColumns => Columns.Where(x => !x.IsUpdatedAudit).ToList();

        /// <summary>
        /// Gets the merge match columns.
        /// </summary>
        public List<DbColumnSchema> MergeMatchColumns => Columns.Where(x => !x.IsCreatedAudit && !x.IsUpdatedAudit && !(UseIdentifierGenerator && x.IsPrimaryKey)).ToList();

        /// <summary>
        /// Gets the merge insert columns.
        /// </summary>
        public List<DbColumnSchema> MergeInsertColumns => Columns.Where(x => !x.IsUpdatedAudit).ToList();

        /// <summary>
        /// Gets the merge update columns.
        /// </summary>
        public List<DbColumnSchema> MergeUpdateColumns => Columns.Where(x => !x.IsCreatedAudit).ToList();

        /// <summary>
        /// Gets the primary key columns.
        /// </summary>
        public List<DbColumnSchema> PrimaryKeyColumns => Columns.Where(x => x.IsPrimaryKey).ToList();

        /// <summary>
        /// Gets the rows.
        /// </summary>
        public List<DataRow> Rows { get; } = [];

        /// <summary>
        /// Gets the column name mappings (from the <see cref="DataParserArgs.TableNameMappings"/> where specified).
        /// </summary>
        public IDictionary<string, string>? ColumnNameMappings { get; }

        /// <summary>
        /// Gets the formatted pre-condition SQL.
        /// </summary>
        public string? PreConditionSql { get; private set; }

        /// <summary>
        /// Gets the formatted pre-SQL.
        /// </summary>
        public string? PreSql { get; private set; }

        /// <summary>
        /// Gets the formatted post-SQL.
        /// </summary>
        public string? PostSql { get; private set; }

        /// <summary>
        /// Adds a row (key value pairs of column name and corresponding value).
        /// </summary>
        /// <param name="row">The row.</param>
        public void AddRow(DataRow row)
        {
            row.ThrowIfNull(nameof(row));

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
            var cds = ParserArgs.ColumnDefaults.GetDefaultsForTable(DbTable);

            for (int i = 0; i < Rows.Count; i++)
            {
                var row = Rows[i];

                // Apply the configured auditing defaults.
                await AddColumnWhereNotSpecifiedAsync(row, DbTable.Migration.Args.CreatedDateColumnName!, () => Task.FromResult<object?>(ParserArgs.DateTimeNow)).ConfigureAwait(false);
                await AddColumnWhereNotSpecifiedAsync(row, DbTable.Migration.Args.CreatedByColumnName!, () => Task.FromResult<object?>(ParserArgs.UserName)).ConfigureAwait(false);
                await AddColumnWhereNotSpecifiedAsync(row, DbTable.Migration.Args.UpdatedDateColumnName!, () => Task.FromResult<object?>(ParserArgs.DateTimeNow)).ConfigureAwait(false);
                await AddColumnWhereNotSpecifiedAsync(row, DbTable.Migration.Args.UpdatedByColumnName!, () => Task.FromResult<object?>(ParserArgs.UserName)).ConfigureAwait(false);

                // Apply an reference data defaults.
                if (IsRefData && ParserArgs.RefDataColumnDefaults != null)
                {
                    foreach (var rdd in ParserArgs.RefDataColumnDefaults)
                    {
                        await AddColumnWhereNotSpecifiedAsync(row, rdd.Key, () => Task.FromResult(rdd.Value(i + 1))).ConfigureAwait(false);
                    }
                }

                // Generate the identifier where specified to do so.
                if (UseIdentifierGenerator)
                {
                    var pkc = DbTable.PrimaryKeyColumns[0];
                    var val = row.Columns.SingleOrDefault(x => x.Name == pkc.Name!);
                    if (val == null)
                    {
                        switch (IdentifierType)
                        {
                            case DataTableIdentifierType.Guid:
                                await AddColumnWhereNotSpecifiedAsync(row, pkc.Name!, async () => await ParserArgs.IdentifierGenerator.GenerateGuidIdentifierAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
                                break;

                            case DataTableIdentifierType.String:
                                await AddColumnWhereNotSpecifiedAsync(row, pkc.Name!, async () => await ParserArgs.IdentifierGenerator.GenerateStringIdentifierAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
                                break;

                            case DataTableIdentifierType.Int:
                                await AddColumnWhereNotSpecifiedAsync(row, pkc.Name!, async () => await ParserArgs.IdentifierGenerator.GenerateInt32IdentifierAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
                                break;

                            case DataTableIdentifierType.Long:
                                await AddColumnWhereNotSpecifiedAsync (row, pkc.Name!, async () => await ParserArgs.IdentifierGenerator.GenerateInt64IdentifierAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
                                break;
                        }
                    }
                }

                // Apply any configured column defaults.
                foreach (var cd in cds)
                {
                    await AddColumnWhereNotSpecifiedAsync(row, cd.Column, () => Task.FromResult(cd.Default(i + 1))).ConfigureAwait(false);
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

        /// <summary>
        /// Applys the <paramref name="config"/>.
        /// </summary>
        /// <param name="config">The <see cref="DataConfig"/>.</param>
        internal void ApplyConfig(DataConfig? config)
        {
            PreConditionSql = config?.PreConditionSql?.Replace("{{schema}}", Schema, StringComparison.OrdinalIgnoreCase).Replace("{{table}}", Name, StringComparison.OrdinalIgnoreCase);
            PreSql = config?.PreSql?.Replace("{{schema}}", Schema, StringComparison.OrdinalIgnoreCase).Replace("{{table}}", Name, StringComparison.OrdinalIgnoreCase);
            PostSql = config?.PostSql?.Replace("{{schema}}", Schema, StringComparison.OrdinalIgnoreCase).Replace("{{table}}", Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}