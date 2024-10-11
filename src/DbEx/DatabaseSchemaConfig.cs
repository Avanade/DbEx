// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx;
using CoreEx.Database;
using CoreEx.Entities;
using CoreEx.RefData;
using DbEx.DbSchema;
using DbEx.Migration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbEx
{
    /// <summary>
    /// Enables database provider specific configuration and capabilities.
    /// </summary>
    /// <param name="migration">The owning <see cref="DatabaseMigrationBase"/>.</param>
    /// <param name="supportsSchema">Indicates whether the database supports per-database schema-based separation.</param>
    /// <param name="defaultSchema">The default schema name used where not explicitly specified.</param>
    public abstract class DatabaseSchemaConfig(DatabaseMigrationBase migration, bool supportsSchema = false, string? defaultSchema = null)
    {
        private readonly string? _defaultSchema = defaultSchema;

        /// <summary>
        /// Gets the owning <see cref="DatabaseMigrationBase"/>.
        /// </summary>
        public DatabaseMigrationBase Migration { get; } = migration.ThrowIfNull(nameof(migration));

        /// <summary>
        /// Indicates whether the database supports per-database schema-based separation.
        /// </summary>
        public bool SupportsSchema { get; } = supportsSchema;

        /// <summary>
        /// Gets the default schema name used where not explicitly specified.
        /// </summary>
        /// <remarks>Will throw an appropriate exception where accessed incorrectly.</remarks>
        public string DefaultSchema => SupportsSchema
            ? (_defaultSchema ?? throw new InvalidOperationException("The database supports per-database schema-based separation and a default is required."))
            : throw new NotSupportedException("The database does not support per-database schema-based separation.");

        /// <summary>
        /// Gets the suffix of the identifier column.
        /// </summary>
        /// <remarks>Where matching reference data columns and the specified column is not found, then the suffix will be appended to the specified column name and an additional match will be performed.</remarks>
        public abstract string IdColumnNameSuffix { get; }

        /// <summary>
        /// Gets the suffix of the code column.
        /// </summary>
        /// <remarks>Where matching reference data columns and the specified column is not found, then the suffix will be appended to the specified column name and an additional match will be performed.</remarks>
        public abstract string CodeColumnNameSuffix { get; }

        /// <summary>
        /// Gets the suffix of the JSON column.
        /// </summary>
        public abstract string JsonColumnNameSuffix { get; }

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
        /// Gets the default <see cref="ILogicallyDeleted.IsDeleted"/> column.
        /// </summary>
        public abstract string IsDeletedColumnName { get; }

        /// <summary>
        /// Gets the default <see cref="IReferenceData.Code"/> column.
        /// </summary>
        public abstract string RefDataCodeColumnName { get; }

        /// <summary>
        /// Gets the default <see cref="IReferenceData.Text"/> column.
        /// </summary>
        public abstract string RefDataTextColumnName { get; }

        /// <summary>
        /// Prepares the <see cref="DatabaseMigrationBase"/> <see cref="MigrationArgs"/> as the final opportunity to finalize any standard defaults.
        /// </summary>
        /// <remarks>Where overriding this base method should be invoked first to perform the standardized preparation.</remarks>
        public virtual void PrepareMigrationArgs()
        {
            // Override/set the values - ensure consistency between the two.
            Migration.Args.IdColumnNameSuffix ??= IdColumnNameSuffix;
            Migration.Args.CodeColumnNameSuffix ??= CodeColumnNameSuffix;
            Migration.Args.JsonColumnNameSuffix ??= JsonColumnNameSuffix;
            Migration.Args.CreatedByColumnName ??= CreatedByColumnName;
            Migration.Args.CreatedDateColumnName ??= CreatedDateColumnName;
            Migration.Args.UpdatedByColumnName ??= UpdatedByColumnName;
            Migration.Args.UpdatedDateColumnName ??= UpdatedDateColumnName;
            Migration.Args.TenantIdColumnName ??= TenantIdColumnName;
            Migration.Args.RowVersionColumnName ??= RowVersionColumnName;
            Migration.Args.IsDeletedColumnName ??= IsDeletedColumnName;
            Migration.Args.RefDataCodeColumnName ??= RefDataCodeColumnName;
            Migration.Args.RefDataTextColumnName ??= RefDataTextColumnName;

            // Where the database has a default schema then this should be ordered first where not already set.
            if (SupportsSchema && !string.IsNullOrEmpty(DefaultSchema) && !Migration.Args.SchemaOrder.Contains(DefaultSchema))
                Migration.Args.SchemaOrder.Insert(0, DefaultSchema);
        }

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
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public virtual Task LoadAdditionalInformationSchema(IDatabase database, List<DbTableSchema> tables, CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Gets the <paramref name="schema"/> and <paramref name="table"/> formatted as the fully qualified name.
        /// </summary>
        /// <param name="schema">The schema name.</param>
        /// <param name="table">The table name.</param>
        /// <returns>The fully qualified name.</returns>
        public abstract string ToFullyQualifiedTableName(string? schema, string table);

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
        /// <param name="value">The value.</param>
        /// <returns>The formatted SQL statement representation.</returns>
        public abstract string ToFormattedSqlStatementValue(DbColumnSchema dbColumnSchema, object? value);
    }
}