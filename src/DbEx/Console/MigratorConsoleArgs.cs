// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.Migration;
using DbEx.Migration.Data;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DbEx.Console
{
    /// <summary>
    /// Provides the <see cref="MigratorConsoleBase"/> arguments.
    /// </summary>
    public class MigratorConsoleArgs : OnRamp.CodeGeneratorDbArgsBase
    {
        /// <summary>
        /// Gets or sets the <see cref="Migration.MigrationCommand"/>.
        /// </summary>
        public MigrationCommand MigrationCommand { get; set; } = MigrationCommand.None;

        /// <summary>
        /// Gets the <see cref="Assembly"/> list to use to probe for assembly resource (in defined sequence); will check this assembly also (no need to explicitly specify).
        /// </summary>
        public List<Assembly> Assemblies { get; } = new List<Assembly> { typeof(MigratorConsoleArgs).Assembly };

        /// <summary>
        /// Adds (inserts) one or more <paramref name="assemblies"/> to <see cref="Assemblies"/> (before any existing values).
        /// </summary>
        /// <param name="assemblies">The assemblies to add.</param>
        /// <remarks>The order in which they are specified is the order in which they will be probed for embedded resources.</remarks>
        /// <returns>The current <see cref="MigratorConsoleArgs"/> instance to support fluent-style method-chaining.</returns>
        public MigratorConsoleArgs AddAssembly(params Assembly[] assemblies)
        {
            foreach (var a in assemblies.Distinct().Reverse())
            {
                if (!Assemblies.Contains(a))
                    Assemblies.Insert(0, a);
            }

            return this;
        }

        /// <summary>
        /// Gets or sets the <see cref="ILogger"/> to optionally log the underlying database migration progress.
        /// </summary>
        public ILogger? Logger { get; set; }

        /// <summary>
        /// Gets or sets the output <see cref="DirectoryInfo"/> where the generated artefacts are to be written.
        /// </summary>
        public DirectoryInfo? OutputDirectory { get; set; }

        /// <summary>
        /// Gets the schema priority list (used to specify schema precedence; otherwise equal last).
        /// </summary>
        public List<string> SchemaOrder { get; } = new List<string>();

        /// <summary>
        /// Adds one or more <paramref name="schemas"/> to the <see cref="SchemaOrder"/>.
        /// </summary>
        /// <param name="schemas">The schemas to add.</param>
        /// <returns>The current <see cref="MigratorConsoleArgs"/> instance to support fluent-style method-chaining.</returns>
        public MigratorConsoleArgs AddSchemaOrder(params string[] schemas)
        {
            SchemaOrder.AddRange(schemas);
            return this;
        }

        /// <summary>
        /// Gets the <see cref="Migration.Data.DataParserArgs"/>.
        /// </summary>
        public DataParserArgs DataParserArgs { get; } = new DataParserArgs();

        /// <summary>
        /// Gets or sets the <see cref="MigrationCommand.Script"/> name.
        /// </summary>
        public string? ScriptName { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="MigrationCommand.Script"/> arguments.
        /// </summary>
        public IDictionary<string, string?>? ScriptArguments { get; set; }
    }
}