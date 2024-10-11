// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.Migration;
using System.Linq;

namespace DbEx.Postgres.Console
{
    /// <summary>
    /// Provides extension methods for <see cref="MigrationArgs"/>.
    /// </summary>
    public static class MigrationArgsExtensions
    {
        /// <summary>
        /// Include the Postgres extended <b>Schema</b> scripts (stored procedures and functions) from <see href="https://github.com/Avanade/DbEx/tree/main/src/DbEx.Postgres/Resources/ExtendedSchema"/>.
        /// </summary>
        /// <param name="args">The <see cref="MigrationArgs"/>.</param>
        /// <returns>The <see cref="MigrationArgs"/> to support fluent-style method-chaining.</returns>
        public static MigrationArgs IncludeExtendedSchemaScripts(this MigrationArgs args)
        {
            AddExtendedSchemaScripts(args);
            return args;
        }

        /// <summary>
        /// Include the Postgres extended <b>Schema</b> scripts (stored procedures and functions) from <see href="https://github.com/Avanade/DbEx/tree/main/src/DbEx.Postgres/Resources/ExtendedSchema"/>.
        /// </summary>
        /// <param name="args">The <see cref="MigrationArgs"/>.</param>
        /// <returns>The <see cref="MigrationArgs"/> to support fluent-style method-chaining.</returns>
        public static void AddExtendedSchemaScripts<TArgs>(TArgs args) where TArgs : MigrationArgsBase<TArgs>
        {
            foreach (var rn in typeof(MigrationArgsExtensions).Assembly.GetManifestResourceNames().Where(x => x.StartsWith("DbEx.Postgres.Resources.ExtendedSchema.") && x.EndsWith(".sql")))
            {
                args.AddScript(MigrationCommand.Schema, typeof(MigrationArgsExtensions).Assembly, rn);
            }
        }
    }
}