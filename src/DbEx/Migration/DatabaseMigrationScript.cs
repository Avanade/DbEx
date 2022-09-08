// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;
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
        /// <param name="file">The <see cref="FileInfo"/>.</param>
        /// <param name="name">The file name.</param>
        public DatabaseMigrationScript(FileInfo file, string name)
        {
            _file = file ?? throw new ArgumentNullException(nameof(file));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseMigrationScript"/> class for an embedded resource.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/>.</param>
        /// <param name="name">The resource name.</param>
        public DatabaseMigrationScript(Assembly assembly, string name)
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseMigrationScript"/> class for the specified <paramref name="sql"/>.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="name">The sql name.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public DatabaseMigrationScript(string sql, string name)
        {
            _sql = sql ?? throw new ArgumentNullException(nameof(sql));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

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
        /// Gets the resource or file <see cref="System.IO.StreamReader"/>.
        /// </summary>
        public StreamReader GetStreamReader() => _assembly is not null 
            ? new StreamReader(_assembly!.GetManifestResourceStream(Name)!) 
            : (_file is not null ? _file!.OpenText() : new StreamReader(new MemoryStream(Encoding.Default.GetBytes(_sql))));
    }
}