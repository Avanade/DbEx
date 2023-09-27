// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.Database;
using CoreEx.Entities;
using CoreEx.RefData;
using DbEx.DbSchema;
using DbEx.Migration.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DbEx
{
    /// <summary>
    /// Enables database provider specific configuration and capabilities.
    /// </summary>
    public abstract class DatabaseSchemaConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseSchemaConfig"/> class.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        /// <param name="supportsSchema">Indicates whether the database supports per-database schema-based separation.</param>
        protected DatabaseSchemaConfig(string databaseName, bool supportsSchema = true)
        {
            DatabaseName = databaseName;
            SupportsSchema = supportsSchema;
            RefDataPredicate = new Func<DbTableSchema, bool>(t => t.Columns.Any(c => c.Name == RefDataCodeColumnName && !c.IsPrimaryKey && c.DotNetType == "string") && t.Columns.Any(c => c.Name == RefDataTextColumnName && !c.IsPrimaryKey && c.DotNetType == "string"));
        }

        /// <summary>
        /// Gets or sets the database name.
        /// </summary>
        /// <remarks>Used to filter schemas for database that do not <see cref="SupportsSchema"/>.</remarks>
        public string DatabaseName { get; }

        /// <summary>
        /// Indicates whether the database supports per-database schema-based separation.
        /// </summary>
        public bool SupportsSchema { get; }

        /// <summary>
        /// Gets the name of the <see cref="IChangeLogAudit.CreatedDate"/> column (where it exists).
        /// </summary>
        public abstract string CreatedDateColumnName { get; }

        /// <summary>
        /// Gets the name of the <see cref="IChangeLogAudit.CreatedBy"/> column (where it exists).
        /// </summary>
        public abstract string CreatedByColumnName { get; }

        /// <summary>
        /// Gets the name of the <see cref="IChangeLogAudit.UpdatedDate"/> column (where it exists).
        /// </summary>
        public abstract string UpdatedDateColumnName { get; }

        /// <summary>
        /// Gets the name of the <see cref="IChangeLogAudit.UpdatedBy"/> column (where it exists).
        /// </summary>
        public abstract string UpdatedByColumnName { get; }

        /// <summary>
        /// Gets the name of the <see cref="ITenantId.TenantId"/> column (where it exists).
        /// </summary>
        public abstract string TenantIdColumnName { get; }

        /// <summary>
        /// Gets the name of the row-version (<see cref="IETag.ETag"/> equivalent) column (where it exists).
        /// </summary>
        public abstract string RowVersionColumnName { get; }

        /// <summary>
        /// Gets the default <see cref="IReferenceData.Code"/> column.
        /// </summary>
        public abstract string RefDataCodeColumnName { get; }

        /// <summary>
        /// Gets the default <see cref="IReferenceData.Text"/> column.
        /// </summary>
        public abstract string RefDataTextColumnName { get; }

        /// <summary>
        /// Gets the default <see cref="ILogicallyDeleted.IsDeleted"/> column.
        /// </summary>
        public abstract string IsDeletedColumnName { get; }

        /// <summary>
        /// Gets the default reference data predicate to determine <see cref="DbTableSchema.IsRefData"/>.
        /// </summary>
        /// <remarks>By default determined by existence of columns named <see cref="RefDataCodeColumnName"/> and <see cref="RefDataTextColumnName"/> (case-insensitive), that are <see cref="DbColumnSchema.IsPrimaryKey"/> equal <c>false</c> 
        /// and <see cref="DbColumnSchema.DotNetType"/> equal '<c>string</c>'.</remarks>
        public virtual Func<DbTableSchema, bool> RefDataPredicate { get; }

        /// <summary>
        /// Gets or sets the suffix of the identifier column where not fully specified.
        /// </summary>
        /// <remarks>Where matching reference data columns and the specified column is not found, then the suffix will be appended to the specified column name and an additional match will be performed.</remarks>
        public abstract string IdColumnNameSuffix { get; }

        /// <summary>
        /// Prepares the <paramref name="dataParserArgs"/> prior to parsing as a final opportunity to finalize any standard defaults.
        /// </summary>
        /// <param name="dataParserArgs">The <see cref="DataParserArgs"/>.</param>
        public abstract void PrepareDataParserArgs(DataParserArgs dataParserArgs);

        /// <summary>
        /// Creates the <see cref="DbColumnSchema"/> from the `InformationSchema.Columns` <see cref="DatabaseRecord"/>.
        /// </summary>
        /// <param name="table">The corresponding <see cref="DbTableSchema"/>.</param>
        /// <param name="dr">The <see cref="DatabaseRecord"/>.</param>
        /// <returns>The <see cref="DbColumnSchema"/>.</returns>
        public abstract DbColumnSchema CreateColumnFromInformationSchema(DbTableSchema table, DatabaseRecord dr);

        /// <summary>
        /// Opportunity to load additional `InformationSchema` related data that is specific to the database.
        /// </summary>
        /// <param name="database">The <see cref="IDatabase"/>.</param>
        /// <param name="tables">The <see cref="DbTableSchema"/> list to load additional data into.</param>
        /// <param name="dataParserArgs">The <see cref="DataParserArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public virtual Task LoadAdditionalInformationSchema(IDatabase database, List<DbTableSchema> tables, DataParserArgs? dataParserArgs, CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Gets the <paramref name="schema"/> and <paramref name="table"/> formatted as the fully qualified name.
        /// </summary>
        /// <param name="schema">The schema name.</param>
        /// <param name="table">The table name.</param>
        /// <returns>The fully qualified name.</returns>
        public abstract string ToFullyQualifiedTableName(string schema, string table);

        /// <summary>
        /// Gets the corresponding .NET <see cref="Type"/> name for the specified <see cref="DbColumnSchema"/>.
        /// </summary>
        /// <param name="schema">The <see cref="DbColumnSchema"/>.</param>
        /// <returns>The .NET <see cref="Type"/> name.</returns>
        public abstract string ToDotNetTypeName(DbColumnSchema schema);

        /// <summary>
        /// Gets the long-form formatted SQL type; includes size, precision, etc.
        /// </summary>
        /// <param name="schema">The <see cref="DbColumnSchema"/>.</param>
        /// <param name="includeNullability">Indicates whether to include the nullability within the formatted value.</param>
        /// <returns>The long-form formatted SQL type.</returns>
        public abstract string ToFormattedSqlType(DbColumnSchema schema, bool includeNullability = true);

        /// <summary>
        /// Gets the formatted SQL statement representation of the <paramref name="value"/>.
        /// </summary>
        /// <param name="dbColumnSchema">The <see cref="DbColumnSchema"/>.</param>
        /// <param name="dataParserArgs">The <see cref="DataParserArgs"/>.</param>
        /// <param name="value">The value.</param>
        /// <returns>The formatted SQL statement representation.</returns>
        public abstract string ToFormattedSqlStatementValue(DbColumnSchema dbColumnSchema, DataParserArgs dataParserArgs, object? value);

        /// <summary>
        /// Indicates whether the <paramref name="dbType"/> is considered an integer.
        /// </summary>
        /// <param name="dbType">The database type.</param>
        /// <returns><c>true</c> indicates it is; otherwise, <c>false</c>.</returns>
        public abstract bool IsDbTypeInteger(string? dbType);

        /// <summary>
        /// Indicates whether the <paramref name="dbType"/> is considered a decimal.
        /// </summary>
        /// <param name="dbType">The database type.</param>
        /// <returns><c>true</c> indicates it is; otherwise, <c>false</c>.</returns>
        public abstract bool IsDbTypeDecimal(string? dbType);

        /// <summary>
        /// Indicates whether the <paramref name="dbType"/> is considered a string.
        /// </summary>
        /// <param name="dbType">The database type.</param>
        /// <returns><c>true</c> indicates it is; otherwise, <c>false</c>.</returns>
        public abstract bool IsDbTypeString(string? dbType);
    }
}