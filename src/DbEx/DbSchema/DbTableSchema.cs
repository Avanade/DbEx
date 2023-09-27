// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.RefData;
using OnRamp.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DbEx.DbSchema
{
    /// <summary>
    /// Represents the Database <b>Table</b> schema definition.
    /// </summary>
    [DebuggerDisplay("{QualifiedName}")]
    public class DbTableSchema
    {
        private string? _dotNetName;
        private string? _pluralName;

        /// <summary>
        /// The <see cref="Regex"/> expression pattern for splitting strings into words.
        /// </summary>
        public const string WordSplitPattern = "([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))";

        /// <summary>
        /// Create an alias from the name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The corresponding alias.</returns>
        /// <remarks>Converts the name into sentence case and takes first character from each word and converts to lowercase; e.g. '<c>SalesOrder</c>' will result in an alias of '<c>so</c>'.</remarks>
        public static string CreateAlias(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var s = StringConverter.ToSentenceCase(name)!;
            return new string(s.Replace(" ", " ").Replace("_", " ").Replace("-", " ").Split(' ').Where(x => !string.IsNullOrEmpty(x)).Select(x => x[..1].ToLower(System.Globalization.CultureInfo.InvariantCulture).ToCharArray()[0]).ToArray());
        }

        /// <summary>
        /// Create a .NET friendly name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="removeKnownSuffix">Indicates whether to remove the known suffix.</param>
        /// <returns>The .NET friendly name.</returns>
        public static string CreateDotNetName(string name, bool removeKnownSuffix = false)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var sb = new StringBuilder();
            name.Split(new char[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries).ForEach(part => sb.Append(StringConverter.ToPascalCase(part)));
            var dotNet = sb.ToString();

            if (removeKnownSuffix)
            {
                var words = Regex.Split(dotNet, WordSplitPattern).Where(x => !string.IsNullOrEmpty(x));
                if (words.Count() > 1 && new string[] { "Id", "Code", "Json" }.Contains(words.Last(), StringComparer.InvariantCultureIgnoreCase))
                    dotNet = string.Join(string.Empty, words.Take(words.Count() - 1));
            }

            return dotNet;
        }

        /// <summary>
        /// Create a plural from the singular name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The pluralized name.</returns>
        public static string CreatePluralName(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var words = Regex.Split(name, WordSplitPattern).Where(x => !string.IsNullOrEmpty(x)).ToList();
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
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var words = Regex.Split(name, WordSplitPattern).Where(x => !string.IsNullOrEmpty(x)).ToList();
            words[^1] = StringConverter.ToSingle(words[^1]);
            return string.Join(string.Empty, words);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbTableSchema"/> class.
        /// </summary>
        /// <param name="config">The database schema configuration.</param>
        /// <param name="schema">The schema name.</param>
        /// <param name="name">The table name.</param>
        public DbTableSchema(DatabaseSchemaConfig config, string schema, string name)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Schema = config.SupportsSchema ? (schema ?? throw new ArgumentNullException(nameof(schema))) : string.Empty;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            QualifiedName = config.ToFullyQualifiedTableName(schema, name);
            Alias = CreateAlias(Name);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbTableSchema"/> class referencing an existing instance.
        /// </summary>
        /// <param name="table">The existing <see cref="DbTableSchema"/>.</param>
        public DbTableSchema(DbTableSchema table)
        {
            Config = table.Config;
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
        /// Gets the <see cref="DatabaseSchemaConfig"/>.
        /// </summary>
        public DatabaseSchemaConfig Config { get; }

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
        /// Gets the schema name.
        /// </summary>
        public string Schema { get; }

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
        public bool IsRefData { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbColumnSchema"/> list.
        /// </summary>
        public List<DbColumnSchema> Columns { get; private set; } = new List<DbColumnSchema>();

        /// <summary>
        /// Gets the primary key <see cref="DbColumnSchema"/> list.
        /// </summary>
        public List<DbColumnSchema> PrimaryKeyColumns => Columns?.Where(x => x.IsPrimaryKey).ToList() ?? new List<DbColumnSchema>();

        /// <summary>
        /// Gets the standard <see cref="DbColumnSchema"/> list (i.e. not primary key, not created audit, not updated audit, not tenant-id, not row-version, not is-deleted).
        /// </summary>
        public List<DbColumnSchema> StandardColumns => Columns?.Where(x => !x.IsPrimaryKey && !x.IsCreatedAudit && !x.IsUpdatedAudit && !x.IsTenantId && !x.IsRowVersion && !x.IsIsDeleted).ToList() ?? new List<DbColumnSchema>();

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
    }
}