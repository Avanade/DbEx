// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;
using System.IO;
using System.Reflection;

namespace DbEx.Migration
{
    /// <summary>
    /// Provides the database migration <see cref="MigrationCommand.Schema"/> script configuration.
    /// </summary>
    public class DatabaseMigrationScript
    {
        /// <summary>
        /// Initializes a new <see cref="DatabaseMigrationScript"/> class for a file.
        /// </summary>
        /// <param name="file">The <see cref="FileInfo"/>.</param>
        /// <param name="name">The resource name.</param>
        public DatabaseMigrationScript(FileInfo file, string name)
        {
            IsResource = false;
            File = file ?? throw new ArgumentNullException(nameof(file));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Initializes a new <see cref="DatabaseMigrationScript"/> class for an embedded resource.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/>.</param>
        /// <param name="name">The resource name.</param>
        public DatabaseMigrationScript(Assembly assembly, string name)
        {
            IsResource = true;
            Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Indicates whether the source of the SQL Script is an embedded <see cref="Assembly"/> resource; otherwise, it is a <see cref="File"/>.
        /// </summary>
        public bool IsResource { get; }

        /// <summary>
        /// Gets the resource name.
        /// </summary>
        /// <remarks>The file name is also formatted as a resource name.</remarks>
        public string Name { get; }

        /// <summary>
        /// Gets the <see cref="System.Reflection.Assembly"/> where <see cref="IsResource"/>.
        /// </summary>
        public Assembly? Assembly { get; }

        /// <summary>
        /// Gets the <see cref="FileInfo"/> where not <see cref="IsResource"/>.
        /// </summary>
        public FileInfo? File { get; }

        /// <summary>
        /// Gets the resource or file <see cref="System.IO.StreamReader"/>.
        /// </summary>
        public StreamReader GetStreamReader() => IsResource ? new StreamReader(Assembly!.GetManifestResourceStream(Name)!) : File!.OpenText();
    }
}