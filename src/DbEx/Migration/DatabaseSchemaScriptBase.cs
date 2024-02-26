// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx;
using System;

namespace DbEx.Migration
{
    /// <summary>
    /// Enables the base database schema script.
    /// </summary>
    /// <param name="migrationScript">The <see cref="DatabaseMigrationScript"/>.</param>
    /// <param name="quotePrefix">The optional quote prefix.</param>
    /// <param name="quoteSuffix">The optional quote suffix.</param>
    public abstract class DatabaseSchemaScriptBase(DatabaseMigrationScript migrationScript, string? quotePrefix = null, string? quoteSuffix = null)
    {
        private string? _schema;
        private string _name = string.Empty;

        /// <summary>
        /// Gets the parent <see cref="DatabaseMigrationScript"/>.
        /// </summary>
        public DatabaseMigrationScript MigrationScript { get; } = migrationScript.ThrowIfNull(nameof(migrationScript));

        /// <summary>
        /// Gets the optional quote prefix.
        /// </summary>
        public string? QuotePrefix { get; } = quotePrefix;

        /// <summary>
        /// Gets the optional quote suffix.
        /// </summary>
        public string? QuoteSuffix { get; } = quoteSuffix;

        /// <summary>
        /// Gets or sets the fully qualified name (as per script).
        /// </summary>
        public string FullyQualifiedName { get; protected set; } = string.Empty;

        /// <summary>
        /// Gets the object type.
        /// </summary>
        public string Type { get; protected set; } = string.Empty;

        /// <summary>
        /// Gets or sets the underlying schema name (where applicable).
        /// </summary>
        /// <remarks>This is schema portion of the <see cref="FullyQualifiedName"/> with any escaping removed.</remarks>
        public string? Schema { get => _schema; protected set => _schema = UnquoteIdentifier(value); }

        /// <summary>
        /// Gets the object name.
        /// </summary>
        /// <remarks>This is name portion of the <see cref="FullyQualifiedName"/> with any escaping removed.</remarks>
        public string Name { get => _name; protected set => _name = UnquoteIdentifier(value)!; }

        /// <summary>
        /// Gets the <see cref="Type"/> order of precedence.
        /// </summary>
        public int TypeOrder { get; internal set; } = -1;

        /// <summary>
        /// Gets or sets the schema order of precedence.
        /// </summary>
        public int SchemaOrder { get; internal set; } = -1;

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string? ErrorMessage { get; internal protected set; }

        /// <summary>
        /// Indicates whether the schema script has an error.
        /// </summary>
        public bool HasError => ErrorMessage != null;

        /// <summary>
        /// Indicates whether the schema script supports a create or replace; i.e. does not require a drop and create as two separate operations.
        /// </summary>
        public bool SupportsReplace { get; protected set; }

        /// <summary>
        /// Gets the corresponding SQL drop statement for the underlying <see cref="Type"/> and <see cref="Name"/>.
        /// </summary>
        public abstract string SqlDropStatement { get; }

        /// <summary>
        /// Gets the corresponding SQL create statement for the underlying <see cref="Type"/> and <see cref="Name"/>.
        /// </summary>
        /// <remarks>This is only used for logging; the original script is invoked.</remarks>
        public abstract string SqlCreateStatement { get; }

        /// <summary>
        /// Unquotes the <paramref name="identifier"/> by removing the <see cref="QuotePrefix"/> and <see cref="QuoteSuffix"/> where they both exist.
        /// </summary>
        /// <param name="identifier">The identifier to unquote.</param>
        /// <returns>The unquoted identifier.</returns>
        public string? UnquoteIdentifier(string? identifier)
            => !string.IsNullOrEmpty(identifier) && !string.IsNullOrEmpty(QuotePrefix) && !string.IsNullOrEmpty(QuoteSuffix) && identifier.StartsWith(QuotePrefix, StringComparison.OrdinalIgnoreCase) && identifier.EndsWith(QuoteSuffix, StringComparison.OrdinalIgnoreCase)
                ? identifier[QuotePrefix.Length..^QuoteSuffix.Length] : identifier;
    }
}