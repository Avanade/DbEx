// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx;
using System.Reflection;

namespace DbEx.Migration
{
    /// <summary>
    /// Provides <see cref="Assembly"/> arguments.
    /// </summary>
    public class MigrationAssemblyArgs
    {
        /// <summary>
        /// Gets or sets the default <b>Data</b> namespace part name.
        /// </summary>
        public static string DefaultDataNamespace { get; set; } = "Data";

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationAssemblyArgs"/>.
        /// </summary>
        /// <param name="assembly">The <see cref="System.Reflection.Assembly"/>.</param>
        /// <param name="dataNamespaces">The <see cref="DataNamespaces"/>; defaults to <see cref="DefaultDataNamespace"/>.</param>
        public MigrationAssemblyArgs(Assembly assembly, params string[] dataNamespaces)
        {
            Assembly = assembly.ThrowIfNull(nameof(Assembly));
            DataNamespaces = dataNamespaces is null || dataNamespaces.Length == 0 ? [DefaultDataNamespace] : dataNamespaces;
        }

        /// <summary>
        /// Gets the <see cref="System.Reflection.Assembly"/>.
        /// </summary>
        public Assembly Assembly { get; }

        /// <summary>
        /// Gets the <b>Data</b> namespace part name(s).
        /// </summary>
        public string[] DataNamespaces { get; }
    }
}