// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.Migration.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DbEx.Migration
{
    /// <summary>
    /// Provides the base <see cref="DatabaseMigrationBase"/> arguments.
    /// </summary>
    public abstract class MigrationArgsBase : OnRamp.CodeGeneratorDbArgsBase
    {
        /// <summary>
        /// Gets or sets the <see cref="DbEx.MigrationCommand"/>.
        /// </summary>
        public MigrationCommand MigrationCommand { get; set; } = MigrationCommand.None;

        /// <summary>
        /// Gets the <see cref="Assembly"/> list to use to probe for assembly resource (in defined sequence); will check this assembly also (no need to explicitly specify).
        /// </summary>
        public List<Assembly> Assemblies { get; } = new List<Assembly> { typeof(MigrationArgs).Assembly };

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
        /// Gets or sets the <see cref="Data.DataParserArgs"/>.
        /// </summary>
        public DataParserArgs DataParserArgs { get; set; } = new DataParserArgs();

        /// <summary>
        /// Gets or sets the <see cref="MigrationCommand.Script"/> name.
        /// </summary>
        public string? ScriptName { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="MigrationCommand.Script"/> arguments.
        /// </summary>
        public IDictionary<string, string?>? ScriptArguments { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="MigrationCommand.Execute"/> statements.
        /// </summary>
        public List<string>? ExecuteStatements { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="MigrationCommand.Reset"/> table filtering predicate.
        /// </summary>
        /// <remarks>This is additional to any pre-configured database provider specified <see cref="DatabaseMigrationBase.DataResetFilterPredicate"/>.</remarks>
        public Func<DbSchema.DbTableSchema, bool>? DataResetFilterPredicate { get; set; }

        /// <summary>
        /// Adds (inserts) one or more <paramref name="assemblies"/> to <see cref="Assemblies"/> (before any existing values).
        /// </summary>
        /// <param name="assemblies">The assemblies to add.</param>
        /// <remarks>The order in which they are specified is the order in which they will be probed for embedded resources.</remarks>
        /// <returns>The current <see cref="MigrationArgsBase"/> instance to support fluent-style method-chaining.</returns>
        public void AddAssembly(params Assembly[] assemblies)
        {
            foreach (var a in assemblies.Distinct().Reverse())
            {
                if (!Assemblies.Contains(a))
                    Assemblies.Insert(0, a);
            }
        }

        /// <summary>
        /// Copy and replace from <paramref name="args"/>.
        /// </summary>
        /// <param name="args">The <see cref="MigrationArgsBase{TSelf}"/> to copy from.</param>
        protected void CopyFrom(MigrationArgsBase args)
        {
            base.CopyFrom(args ?? throw new ArgumentNullException(nameof(args)));

            MigrationCommand = args.MigrationCommand;
            Assemblies.Clear();
            Assemblies.AddRange(args.Assemblies);
            Logger = args.Logger;
            OutputDirectory = args.OutputDirectory;
            SchemaOrder.Clear();
            SchemaOrder.AddRange(args.SchemaOrder);
            DataParserArgs.CopyFrom(args.DataParserArgs);
            ScriptName = args.ScriptName;
            DataResetFilterPredicate = args.DataResetFilterPredicate;

            if (args.ScriptArguments == null)
                ScriptArguments = null;
            else
            {
                ScriptArguments = new Dictionary<string, string?>();
                args.ScriptArguments.ForEach(x => ScriptArguments.Add(x.Key, x.Value));
            }

            if (args.ExecuteStatements == null)
                ExecuteStatements = null;
            else
            {
                ExecuteStatements = new();
                ExecuteStatements.AddRange(args.ExecuteStatements);
            }
        }
    }
}