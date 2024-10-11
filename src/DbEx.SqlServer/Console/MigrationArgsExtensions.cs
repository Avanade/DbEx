// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.Migration;
using System;
using System.Linq;

namespace DbEx.SqlServer.Console
{
    /// <summary>
    /// Provides extension methods for <see cref="MigrationArgs"/>.
    /// </summary>
    public static class MigrationArgsExtensions
    {
        /// <summary>
        /// Include the SQL Server extended <b>Schema</b> scripts (stored procedures and functions) from <see href="https://github.com/Avanade/DbEx/tree/main/src/DbEx.SqlServer/Resources/ExtendedSchema"/>.
        /// </summary>
        /// <param name="args">The <see cref="MigrationArgs"/>.</param>
        /// <returns>The <see cref="MigrationArgs"/> to support fluent-style method-chaining.</returns>
        public static MigrationArgs IncludeExtendedSchemaScripts(this MigrationArgs args)
        {
            AddExtendedSchemaScripts(args);
            return args;
        }

        /// <summary>
        /// Adds the SQL Server extended <b>Schema</b> scripts (stored procedures and functions) from <see href="https://github.com/Avanade/DbEx/tree/main/src/DbEx.SqlServer/Resources/ExtendedSchema"/>.
        /// </summary>
        /// <typeparam name="TArgs">The <see cref="MigrationArgsBase{TSelf}"/> <see cref="Type"/>.</typeparam>
        /// <param name="args">The <see cref="MigrationArgsBase{TSelf}"/>.</param>
        public static void AddExtendedSchemaScripts<TArgs>(TArgs args) where TArgs : MigrationArgsBase<TArgs>
        {
            foreach (var rn in typeof(MigrationArgsExtensions).Assembly.GetManifestResourceNames().Where(x => x.StartsWith("DbEx.SqlServer.Resources.ExtendedSchema.") && x.EndsWith(".sql")))
            {
                args.AddScript(MigrationCommand.Schema, typeof(MigrationArgsExtensions).Assembly, rn);
            }
        }
    }
}