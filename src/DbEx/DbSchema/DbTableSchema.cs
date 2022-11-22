// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using OnRamp.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace DbEx.DbSchema
{
    /// <summary>
    /// Represents the Database <b>Table</b> schema definition.
    /// </summary>
    [DebuggerDisplay("{QualifiedName}")]
    public class DbTableSchema
    {
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
        /// Initializes a new instance of the <see cref="DbTableSchema"/> class.
        /// </summary>
        /// <param name="config">The database schema configuration.</param>
        /// <param name="schema">The schema name.</param>
        /// <param name="name">The table name.</param>
        public DbTableSchema(DbDatabaseSchemaConfig config, string schema, string name)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Schema = config.SupportsSchema ? (schema ?? throw new ArgumentNullException(nameof(schema))) : string.Empty;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            QualifiedName = config.GetFullyQualifiedTableName(schema, name);
            Alias = CreateAlias(Name);
        }

        /// <summary>
        /// Gets the <see cref="DbDatabaseSchemaConfig"/>.
        /// </summary>
        public DbDatabaseSchemaConfig Config { get; }

        /// <summary>
        /// Gets the table name.
        /// </summary>
        public string Name { get; }

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
    }
}