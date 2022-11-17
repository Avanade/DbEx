// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;

namespace DbEx.Migration
{
    /// <summary>
    /// Enables the base database schema script.
    /// </summary>
    public abstract class DatabaseSchemaScriptBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseSchemaScriptBase"/> class.
        /// </summary>
        /// <param name="migrationScript">The <see cref="DatabaseMigrationScript"/>.</param>
        public DatabaseSchemaScriptBase(DatabaseMigrationScript migrationScript) => MigrationScript = migrationScript ?? throw new ArgumentNullException(nameof(migrationScript));

        /// <summary>
        /// Gets the parent <see cref="DatabaseMigrationScript"/>.
        /// </summary>
        public DatabaseMigrationScript MigrationScript { get; }

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
        public string? Schema { get; protected set; }

        /// <summary>
        /// Gets the object name.
        /// </summary>
        /// <remarks>This is name portion of the <see cref="FullyQualifiedName"/> with any escaping removed.</remarks>
        public string Name { get; protected set; } = string.Empty;

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
        /// Gets the corresponding SQL drop statement for the underlying <see cref="Type"/> and <see cref="Name"/>.
        /// </summary>
        public abstract string SqlDropStatement { get; }

        /// <summary>
        /// Gets the corresponding SQL create statement for the underlying <see cref="Type"/> and <see cref="Name"/>.
        /// </summary>
        /// <remarks>This is only used for logging; the original script is invoked.</remarks>
        public abstract string SqlCreateStatement { get; }
    }
}