// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.Entities;
using CoreEx.RefData;
using DbEx.DbSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DbEx.Migration.Data
{
    /// <summary>
    /// Represents the <see cref="DataParser"/> arguments.
    /// </summary>
    public class DataParserArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataParserArgs"/> class.
        /// </summary>
        public DataParserArgs() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataParserArgs"/> class with a pre-existing <paramref name="parameters"/> reference.
        /// </summary>
        /// <param name="parameters">The parameters reference.</param>
        public DataParserArgs(Dictionary<string, object?> parameters) => Parameters = parameters;

        /// <summary>
        /// Gets or sets the user name.
        /// </summary>
        /// <remarks>Defaults to '<c><see cref="Environment.UserDomainName"/>/<see cref="Environment.UserName"/></c>'.</remarks>
        public string UserName { get; set; } = Environment.UserDomainName == null ? Environment.UserName : $"{Environment.UserDomainName}\\{Environment.UserName}";

        /// <summary>
        /// Gets or sets the current <see cref="DateTime"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="DateTime.UtcNow"/>.</remarks>
        public DateTime DateTimeNow { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the suffix of the identifier column where not fully specified.
        /// </summary>
        /// <remarks>Where matching columns and the specified column is not found, then the suffix will be appended to the specified column name and an additional match will be performed.
        /// <para>Defaults to <see cref="DatabaseSchemaConfig.IdColumnNameSuffix"/> where not specified (i.e. <c>null</c>).</para></remarks>
        public string? IdColumnNameSuffix { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="IChangeLogAudit.CreatedDate"/> column (where it exists).
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.CreatedDateColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? CreatedDateColumnName { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="IChangeLogAudit.CreatedBy"/> column (where it exists).
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.CreatedByColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? CreatedByColumnName { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="IChangeLogAudit.UpdatedDate"/> column (where it exists).
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.UpdatedDateColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? UpdatedDateColumnName { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="IChangeLogAudit.UpdatedBy"/> column (where it exists).
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.UpdatedByColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? UpdatedByColumnName { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="ITenantId.TenantId"/> column (where it exists).
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.TenantIdColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? TenantIdColumnName { get; set; }

        /// <summary>
        /// Gets or sets the name of the row-version (<see cref="IETag.ETag"/> equivalent) column (where it exists).
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.RowVersionColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? RowVersionColumnName { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="ILogicallyDeleted.IsDeleted"/> column (where it exists).
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.IsDeletedColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? IsDeletedColumnName { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="IReferenceData.Code"/> column.
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.RefDataCodeColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? RefDataCodeColumnName { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="IReferenceData.Text"/> column.
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.RefDataTextColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? RefDataTextColumnName { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IIdentifierGenerator"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="GuidIdentifierGenerator"/>.</remarks>
        public IIdentifierGenerator IdentifierGenerator { get; set; } = new GuidIdentifierGenerator();

        /// <summary>
        /// Gets or sets the <see cref="DateTime"/> format.
        /// </summary>
        /// <remarks>Defaults to '<c>yyyy-MM-ddTHH:mm:ss.fffffff</c>'.</remarks>
        public string DateTimeFormat { get; set; } = "yyyy-MM-ddTHH:mm:ss.fffffff";

        /// <summary>
        /// Gets or sets the reference data column defaults dictionary.
        /// </summary>
        /// <remarks>The list should contain the column name and function that returns the default value (the input to the function is the corresponding row count as specified).</remarks>
        public Dictionary<string, Func<int, object?>> RefDataColumnDefaults { get; } = new Dictionary<string, Func<int, object?>>();

        /// <summary>
        /// Adds a reference data column default to the <see cref="RefDataColumnDefaults"/>.
        /// </summary>
        /// <param name="column">The column name.</param>
        /// <param name="default">The function that provides the default value.</param>
        /// <returns>The <see cref="DataParserArgs"/> to support fluent-style method-chaining.</returns>
        public DataParserArgs RefDataColumnDefault(string column, Func<int, object?> @default)
        {
            RefDataColumnDefaults.Add(column, @default);
            return this;
        }

        /// <summary>
        /// Gets or sets the column defaults collection.
        /// </summary>
        /// <remarks>The list should contain the column name and function that returns the default value (the input to the function is the corresponding row count as specified).</remarks>
        public DataParserColumnDefaultCollection ColumnDefaults { get; } = new DataParserColumnDefaultCollection();

        /// <summary>
        /// Adds a <see cref="DataParserColumnDefault"/> to the <see cref="ColumnDefaults"/>.
        /// </summary>
        /// <param name="schema">The schema name; a '<c>*</c>' denotes any schema.</param>
        /// <param name="table">The table name; a '<c>*</c>' denotes any table.</param>
        /// <param name="column">The name of the column to be updated.</param>
        /// <param name="default">The function that provides the default value.</param>
        /// <returns>The <see cref="DataParserArgs"/> to support fluent-style method-chaining.</returns>
        public DataParserArgs ColumnDefault(string schema, string table, string column, Func<int, object?> @default)
        {
            ColumnDefaults.Add(new DataParserColumnDefault(schema, table, column, @default));
            return this;
        }

        /// <summary>
        /// Gets the runtime parameters.
        /// </summary>
        public Dictionary<string, object?> Parameters { get; } = new Dictionary<string, object?>();

        /// <summary>
        /// Adds a parameter to the <see cref="MigrationArgsBase.Parameters"/> where it does not already exist; unless <paramref name="overrideExisting"/> is selected then it will add or override.
        /// </summary>
        /// <param name="key">The parameter key.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="overrideExisting">Indicates whether to override the existing value where it is pre-existing; otherwise, will not add/update.</param>
        /// <returns>The <see cref="DataParserArgs"/> to support fluent-style method-chaining.</returns>
        public DataParserArgs Parameter(string key, object? value, bool overrideExisting = false)
        {
            if (!Parameters.TryAdd(key, value) && overrideExisting)
                Parameters[key] = value;

            return this;
        }

        /// <summary>
        /// Gets or sets the <see cref="DbTableSchema"/> updater.
        /// </summary>
        /// <remarks>This is invoked offering an opportunity to further update (manipulate) the <see cref="DbTableSchema"/> selected from the database using the <see cref="DatabaseExtensions.SelectSchemaAsync"/>.</remarks>
        public Func<IEnumerable<DbTableSchema>, CancellationToken, Task<IEnumerable<DbTableSchema>>>? DbSchemaUpdaterAsync { get; set; }

        /// <summary>
        /// Copy and replace from <paramref name="args"/>.
        /// </summary>
        /// <param name="args">The <see cref="DataParserArgs"/> to copy from.</param>
        public void CopyFrom(DataParserArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            UserName = args.UserName;
            DateTimeNow = args.DateTimeNow;
            IdColumnNameSuffix = args.IdColumnNameSuffix;
            CreatedDateColumnName = args.CreatedDateColumnName;
            CreatedByColumnName = args.CreatedByColumnName;
            UpdatedDateColumnName = args.UpdatedDateColumnName;
            UpdatedByColumnName = args.UpdatedByColumnName;
            RowVersionColumnName = args.RowVersionColumnName;
            TenantIdColumnName = args.TenantIdColumnName;
            RefDataCodeColumnName = args.RefDataCodeColumnName;
            RefDataTextColumnName = args.RefDataTextColumnName;
            IdentifierGenerator = args.IdentifierGenerator;
            DateTimeFormat = args.DateTimeFormat;
            DbSchemaUpdaterAsync = args.DbSchemaUpdaterAsync;
            RefDataColumnDefaults.Clear();
            args.RefDataColumnDefaults.ForEach(x => RefDataColumnDefaults.Add(x.Key, x.Value));
            ColumnDefaults.Clear();
            args.ColumnDefaults.ForEach(ColumnDefaults.Add);
            Parameters.Clear();
            args.Parameters.ForEach(x => Parameters.Add(x.Key, x.Value));
        }
    }
}