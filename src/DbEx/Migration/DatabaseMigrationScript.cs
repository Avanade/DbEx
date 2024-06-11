// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx;
using System.IO;
using System.Reflection;
using System.Text;

namespace DbEx.Migration
{
    /// <summary>
    /// Provides the database migration <see cref="MigrationCommand.Schema"/> script configuration.
    /// </summary>
    public class DatabaseMigrationScript
    {
        private readonly FileInfo? _file;
        private readonly Assembly? _assembly;
        private readonly string? _sql;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseMigrationScript"/> class for a <paramref name="file"/>.
        /// </summary>
        /// <param name="databaseMigation">The owning <see cref="DatabaseMigrationBase"/>.</param>
        /// <param name="file">The <see cref="FileInfo"/>.</param>
        /// <param name="name">The file name.</param>
        public DatabaseMigrationScript(DatabaseMigrationBase databaseMigation, FileInfo file, string name)
        {
            DatabaseMigration = databaseMigation.ThrowIfNull(nameof(databaseMigation));
            _file = file.ThrowIfNull(nameof(file));
            Name = name.ThrowIfNullOrEmpty(nameof(name));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseMigrationScript"/> class for an embedded resource.
        /// </summary>
        /// <param name="databaseMigation">The owning <see cref="DatabaseMigrationBase"/>.</param>
        /// <param name="assembly">The <see cref="Assembly"/>.</param>
        /// <param name="name">The resource name.</param>
        public DatabaseMigrationScript(DatabaseMigrationBase databaseMigation, Assembly assembly, string name)
        {
            DatabaseMigration = databaseMigation.ThrowIfNull(nameof(databaseMigation));
            _assembly = assembly.ThrowIfNull(nameof(assembly));
            Name = name.ThrowIfNullOrEmpty(nameof(name));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseMigrationScript"/> class for the specified <paramref name="sql"/>.
        /// </summary>
        /// <param name="databaseMigation">The owning <see cref="DatabaseMigrationBase"/>.</param>
        /// <param name="sql">The SQL statement.</param>
        /// <param name="name">The sql name.</param>
        public DatabaseMigrationScript(DatabaseMigrationBase databaseMigation, string sql, string name)
        {
            DatabaseMigration = databaseMigation.ThrowIfNull(nameof(databaseMigation));
            _sql = sql.ThrowIfNull(nameof(sql));
            Name = name.ThrowIfNullOrEmpty(nameof(name));
        }

        /// <summary>
        /// Gets the owning <see cref="DatabaseMigrationBase"/>.
        /// </summary>
        public DatabaseMigrationBase DatabaseMigration { get; }

        /// <summary>
        /// Gets the name used for journaling.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the group order for the script.
        /// </summary>
        public int GroupOrder { get; set; }

        /// <summary>
        /// Indicates whether the script is run once or always.
        /// </summary>
        /// <remarks><c>true</c> to run always; otherwise, <c>false</c> to run once (default).</remarks>
        public bool RunAlways { get; set; }

        /// <summary>
        /// Gets or sets additional tag text to output to the log.
        /// </summary>
        public string? Tag { get; set; }

        /// <summary>
        /// Gets the underlying SQL statement source.
        /// </summary>
        public string Source => _assembly is not null ? "RES" : (_file is not null ? "FILE" : "SQL");

        /// <summary>
        /// Gets the resource or file <see cref="System.IO.StreamReader"/>.
        /// </summary>
        public StreamReader GetStreamReader() => _assembly is not null 
            ? new StreamReader(_assembly!.GetManifestResourceStream(Name)!) 
            : (_file is not null ? _file!.OpenText() : new StreamReader(new MemoryStream(Encoding.Default.GetBytes(_sql!))));
    }
}