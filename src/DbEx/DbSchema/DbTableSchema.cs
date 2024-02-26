// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx;
using CoreEx.RefData;
using CoreEx.Text;
using DbEx.Migration;
using OnRamp.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DbEx.DbSchema
{
    /// <summary>
    /// Represents the Database <b>Table</b> schema definition.
    /// </summary>
    [DebuggerDisplay("{QualifiedName}")]
    public partial class DbTableSchema
    {
        private static readonly char[] _separators = ['_', '-'];
        private static readonly string[] _suffixes = ["Id", "Code", "Json"];

        private string? _dotNetName;
        private string? _pluralName;

        /// <summary>
        /// Create an alias from the name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The corresponding alias.</returns>
        /// <remarks>Converts the name into sentence case and takes first character from each word and converts to lowercase; e.g. '<c>SalesOrder</c>' will result in an alias of '<c>so</c>'.</remarks>
        public static string CreateAlias(string name)
        {
            name.ThrowIfNullOrEmpty(nameof(name));
            var s = StringConverter.ToSentenceCase(name)!;
            return new string(s.Replace(" ", " ").Replace("_", " ").Replace("-", " ").Split(' ').Where(x => !string.IsNullOrEmpty(x)).Select(x => x[..1].ToLower(System.Globalization.CultureInfo.InvariantCulture).ToCharArray()[0]).ToArray());
        }

        /// <summary>
        /// Create a .NET friendly name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The .NET friendly name.</returns>
        /// <remarks>Removes any snake/camel case separator characters and converts each separated work into Pascal case before combining.</remarks>
        public static string CreateDotNetName(string name)
        {
            name.ThrowIfNullOrEmpty(nameof(name));
            var sb = new StringBuilder();
            name.Split(_separators, StringSplitOptions.RemoveEmptyEntries).ForEach(part => sb.Append(StringConverter.ToPascalCase(part)));
            return sb.ToString();
        }

        /// <summary>
        /// Create a plural from the singular name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The pluralized name.</returns>
        public static string CreatePluralName(string name)
        {
            name.ThrowIfNullOrEmpty(nameof(name));
            var words = SentenceCase.SplitIntoWords(name).Where(x => !string.IsNullOrEmpty(x)).ToList();
            words[^1] = StringConverter.ToPlural(words[^1]);
            return string.Join(string.Empty, words);
        }

        /// <summary>
        /// Create a singular from the pluralized name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The singular name.</returns>
        public static string CreateSingularName(string name)
        {
            name.ThrowIfNullOrEmpty(nameof(name));
            var words = SentenceCase.SplitIntoWords(name).Where(x => !string.IsNullOrEmpty(x)).ToList();
            words[^1] = StringConverter.ToSingle(words[^1]);
            return string.Join(string.Empty, words);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbTableSchema"/> class.
        /// </summary>
        /// <param name="migration">The <see cref="DatabaseMigrationBase"/>.</param>
        /// <param name="schema">The schema name.</param>
        /// <param name="name">The table name.</param>
        public DbTableSchema(DatabaseMigrationBase migration, string? schema, string name)
        {
            Migration = migration.ThrowIfNull(nameof(migration));
            Schema = Migration.SchemaConfig.SupportsSchema ? schema.ThrowIfNull(nameof(schema)) : string.Empty;
            Name = name.ThrowIfNullOrEmpty(nameof(name));
            QualifiedName = Migration.SchemaConfig.ToFullyQualifiedTableName(schema, name);
            Alias = CreateAlias(Name);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbTableSchema"/> class referencing an existing instance.
        /// </summary>
        /// <param name="table">The existing <see cref="DbTableSchema"/>.</param>
        public DbTableSchema(DbTableSchema table)
        {
            Migration = table.Migration;
            Schema = table.Schema;
            Name = table.Name;
            QualifiedName = table.QualifiedName;
            Alias = table.Alias;
            IsAView = table.IsAView;
            IsRefData = table.IsRefData;
            Columns.AddRange(table.Columns);
            RefDataCodeColumn = table.RefDataCodeColumn;
        }

        /// <summary>
        /// Gets the schema name.
        /// </summary>
        public string Schema { get; }

        /// <summary>
        /// Gets the <see cref="DatabaseMigrationBase"/>.
        /// </summary>
        public DatabaseMigrationBase Migration { get; }

        /// <summary>
        /// Gets the table name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the table name in .NET friendly form.
        /// </summary>
        public string DotNetName => _dotNetName ??= CreateDotNetName(Name);

        /// <summary>
        /// Gets the <see cref="DotNetName"/> in plural form.
        /// </summary>
        public string PluralName => _pluralName ??= CreatePluralName(DotNetName);

        /// <summary>
        /// Gets or sets the alias (automatically updated from the <see cref="Name"/> when instantiated).
        /// </summary>
        public string? Alias { get; set; }

        /// <summary>
        /// Gets the fully qualified name for the database.
        /// </summary>
        public string? QualifiedName { get; }

        /// <summary>
        /// Indicates whether the Table is actually a View.
        /// </summary>
        public bool IsAView { get; set; }

        /// <summary>
        /// Indicates whether the Table is considered reference data.
        /// </summary>
        /// <remarks>By default determined by existence of columns named <see cref="MigrationArgsBase.RefDataCodeColumnName"/> and <see cref="MigrationArgsBase.RefDataTextColumnName"/>, that are <see cref="DbColumnSchema.IsPrimaryKey"/> equal <c>false</c> 
        /// and <see cref="DbColumnSchema.DotNetType"/> equal '<c>string</c>'.</remarks>
        public bool IsRefData { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbColumnSchema"/> list.
        /// </summary>
        public List<DbColumnSchema> Columns { get; private set; } = [];

        /// <summary>
        /// Gets the primary key <see cref="DbColumnSchema"/> list.
        /// </summary>
        public List<DbColumnSchema> PrimaryKeyColumns => Columns?.Where(x => x.IsPrimaryKey).ToList() ?? [];

        /// <summary>
        /// Gets the standard <see cref="DbColumnSchema"/> list (i.e. not primary key, not created audit, not updated audit, not tenant-id, not row-version, not is-deleted).
        /// </summary>
        public List<DbColumnSchema> StandardColumns => Columns?.Where(x => !x.IsPrimaryKey && !x.IsCreatedAudit && !x.IsUpdatedAudit && !x.IsTenantId && !x.IsRowVersion && !x.IsIsDeleted).ToList() ?? [];

        /// <summary>
        /// Gets the tenant idenfifier <see cref="DbColumnSchema"/> (if any).
        /// </summary>
        public DbColumnSchema? TenantIdColumn => Columns?.FirstOrDefault(x => x.IsTenantId);

        /// <summary>
        /// Gets the row version <see cref="DbColumnSchema"/> (if any).
        /// </summary>
        public DbColumnSchema? RowVersionColumn => Columns?.FirstOrDefault(x => x.IsRowVersion);

        /// <summary>
        /// Gets the is-deleted <see cref="DbColumnSchema"/> (if any).
        /// </summary>
        public DbColumnSchema? IsDeletedColumn => Columns?.FirstOrDefault(x => x.IsIsDeleted);

        /// <summary>
        /// Indicates whether the table has any audit columns.
        /// </summary>
        public bool HasAuditColumns => Columns?.Any(x => x.IsCreatedAudit || x.IsUpdatedAudit) ?? false;

        /// <summary>
        /// Gets or sets the <see cref="IReferenceData.Code"/> <see cref="DbColumnSchema"/>.
        /// </summary>
        public DbColumnSchema? RefDataCodeColumn { get; set; }

        /// <summary>
        /// Gets the <see cref="DbColumnSchema"/> list that are part of a constraint (i.e. unique or foreign key).
        /// </summary>
        public List<DbColumnSchema> ConstraintColumns => Columns?.Where(x => x.IsUnique || !string.IsNullOrEmpty(x.ForeignTable)).ToList() ?? [];
    }
}