// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx;
using CoreEx.Entities;
using CoreEx.RefData;
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
        private readonly List<MigrationAssemblyArgs> _assemblies = [new MigrationAssemblyArgs(typeof(MigrationArgsBase).Assembly)];

        /// <summary>
        /// Gets the <see cref="DatabaseMigrationBase.DatabaseName"/> <see cref="Parameters"/> name.
        /// </summary>
        public const string DatabaseNameParamName = "DatabaseName";

        /// <summary>
        /// Gets the <see cref="DatabaseMigrationBase.Journal"/> <see cref="IDatabaseJournal.Schema"/> <see cref="Parameters"/> name.
        /// </summary>
        public const string JournalSchemaParamName = "JournalSchema";

        /// <summary>
        /// Gets the <see cref="DatabaseMigrationBase.Journal"/> <see cref="IDatabaseJournal.Table"/> <see cref="Parameters"/> name.
        /// </summary>
        public const string JournalTableParamName = "JournalTable";

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationArgsBase"/>.
        /// </summary>
        public MigrationArgsBase() => DataParserArgs = new DataParserArgs(Parameters);

        /// <summary>
        /// Gets or sets the <see cref="DbEx.MigrationCommand"/>.
        /// </summary>
        public MigrationCommand MigrationCommand { get; set; } = MigrationCommand.None;

        /// <summary>
        /// Gets the <see cref="Assembly"/> list to use to probe for assembly resource (in defined sequence); will automatically add this (DbEx) assembly also (therefore no need to explicitly specify).
        /// </summary>
        public IEnumerable<MigrationAssemblyArgs> Assemblies => _assemblies;

        /// <summary>
        /// Gets the <see cref="Assemblies"/> reversed in order for probe-based sequencing.
        /// </summary>
        public IEnumerable<MigrationAssemblyArgs> ProbeAssemblies => Assemblies.Reverse();

        /// <summary>
        /// Gets the runtime parameters.
        /// </summary>
        /// <remarks>The following parameter names are reserved for a specific internal purpose: <see cref="DatabaseNameParamName"/>, <see cref="JournalSchemaParamName"/> and <see cref="JournalTableParamName"/>.
        /// <para><see cref="MigrationCommand.Script"/> and <see cref="MigrationCommand.CodeGen"/> can support additional command-line arguments; these are automatically added as '<c>ParamN</c>' where '<c>N</c>' is the zero-based index; e.g. '<c>Param0</c>'.</para></remarks>
        public Dictionary<string, object?> Parameters { get; } = [];

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
        public List<string> SchemaOrder { get; } = [];

        /// <summary>
        /// Gets or sets the <see cref="Data.DataParserArgs"/>.
        /// </summary>
        public DataParserArgs DataParserArgs { get; set; }

        /// <summary>
        /// Gets or sets the suffix of the 'Id' (identifier) column.
        /// </summary>
        /// <remarks>Where matching columns and the specified column is not found, then the suffix will be appended to the specified column name and an additional match will be performed.
        /// <para>Defaults to <see cref="DatabaseSchemaConfig.IdColumnNameSuffix"/> where not specified (i.e. <c>null</c>).</para></remarks>
        public string? IdColumnNameSuffix { get; set; }

        /// <summary>
        /// Gets or sets the suffix of the 'Code' column.
        /// </summary>
        /// <remarks>Where matching columns and the specified column is not found, then the suffix will be appended to the specified column name and an additional match will be performed.
        /// <para>Defaults to <see cref="DatabaseSchemaConfig.CodeColumnNameSuffix"/> where not specified (i.e. <c>null</c>).</para></remarks>
        public string? CodeColumnNameSuffix { get; set; }

        /// <summary>
        /// Gets or sets the suffix of the 'Json' column.
        /// </summary>
        /// <remarks>Where matching columns and the specified column is not found, then the suffix will be appended to the specified column name and an additional match will be performed.
        /// <para>Defaults to <see cref="DatabaseSchemaConfig.JsonColumnNameSuffix"/> where not specified (i.e. <c>null</c>).</para></remarks>
        public string? JsonColumnNameSuffix { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="IChangeLogAudit.CreatedDate"/> column (where it exists).
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.CreatedDateColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? CreatedDateColumnName { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="IChangeLogAudit.CreatedBy"/> column (where it exists).
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.CreatedByColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? CreatedByColumnName { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="IChangeLogAudit.UpdatedDate"/> column (where it exists).
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.UpdatedDateColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? UpdatedDateColumnName { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="IChangeLogAudit.UpdatedBy"/> column (where it exists).
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.UpdatedByColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? UpdatedByColumnName { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="ITenantId.TenantId"/> column (where it exists).
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.TenantIdColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? TenantIdColumnName { get; set; }

        /// <summary>
        /// Gets or sets the name of the row-version (<see cref="IETag.ETag"/> equivalent) column (where it exists).
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.RowVersionColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? RowVersionColumnName { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="ILogicallyDeleted.IsDeleted"/> column (where it exists).
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.IsDeletedColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? IsDeletedColumnName { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="IReferenceData.Code"/> column.
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.RefDataCodeColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? RefDataCodeColumnName { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="IReferenceData.Text"/> column.
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.RefDataTextColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? RefDataTextColumnName { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="MigrationCommand.Execute"/> statements.
        /// </summary>
        public List<string>? ExecuteStatements { get; set; }

        /// <summary>
        /// Indicates whether to automatically accept any confirmation prompts (command-line execution only).
        /// </summary>
        public bool AcceptPrompts { get; set; }

        /// <summary>
        /// Indicates whether to drop all the known schema objects before creating them.
        /// </summary>
        public bool DropSchemaObjects { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="MigrationCommand.Reset"/> table filtering predicate.
        /// </summary>
        /// <remarks>This is additional to any pre-configured database provider specified <see cref="DatabaseMigrationBase.DataResetFilterPredicate"/>.</remarks>
        public Func<DbSchema.DbTableSchema, bool>? DataResetFilterPredicate { get; set; }

#if NET7_0_OR_GREATER
        /// <summary>
        /// Indicates whether to emit the <see cref="DbSchema.DbColumnSchema.DotNetType"/> as a <see cref="DateOnly"/> where <see cref="DbSchema.DbColumnSchema.IsDotNetDateOnly"/>; otherwise, as a <see cref="DateTime"/> (default).
        /// </summary>
#else
        /// <summary>
        /// Indicates whether to emit the <see cref="DbSchema.DbColumnSchema.DotNetType"/> as a <c>DateOnly</c> where <see cref="DbSchema.DbColumnSchema.IsDotNetDateOnly"/>; otherwise, as a <see cref="DateTime"/> (default).
        /// </summary>
#endif
        public bool EmitDotNetDateOnly { get; set; }

#if NET7_0_OR_GREATER
        /// <summary>
        /// Indicates whether to emit the <see cref="DbSchema.DbColumnSchema.DotNetType"/> as a <see cref="TimeOnly"/> where <see cref="DbSchema.DbColumnSchema.IsDotNetTimeOnly"/>; otherwise, as a <see cref="DateTime"/> (default).
        /// </summary>
#else
        /// <summary>
        /// Indicates whether to emit the <see cref="DbSchema.DbColumnSchema.DotNetType"/> as a <c>TimeOnly</c> where <see cref="DbSchema.DbColumnSchema.IsDotNetTimeOnly"/>; otherwise, as a <see cref="DateTime"/> (default).
        /// </summary>
#endif
        public bool EmitDotNetTimeOnly { get; set; }

        /// <summary>
        /// Clears the <see cref="Assemblies"/> by removing all existing items.
        /// </summary>
        public void ClearAssemblies() => _assemblies.Clear();

        /// <summary>
        /// Adds one or more <paramref name="assemblies"/> to the <see cref="Assemblies"/>.
        /// </summary>
        /// <param name="assemblies">The assemblies to add.</param>
        /// <remarks>Where a specified <see cref="Assembly"/> item already exists within the <see cref="Assemblies"/> it will not be added again.</remarks>
        public void AddAssembly(params Assembly[] assemblies) => AddAssembly(assemblies.Select(x => new MigrationAssemblyArgs(x)).ToArray());

        /// <summary>
        /// Adds one or more <paramref name="assemblies"/> to the <see cref="Assemblies"/>.
        /// </summary>
        /// <param name="assemblies">The assemblies to add.</param>
        /// <remarks>Where a specified <see cref="Assembly"/> item already exists within the <see cref="Assemblies"/> it will not be added again.</remarks>
        public void AddAssembly(params MigrationAssemblyArgs[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                if (!_assemblies.Any(x => x.Assembly == assembly.Assembly))
                    _assemblies.Add(assembly);
            }
        }

        /// <summary>
        /// Adds one or more <paramref name="assemblies"/> to the <see cref="Assemblies"/> after the specified <paramref name="assemblyToFind"/>; where not found, will be added to the end.
        /// </summary>
        /// <param name="assemblyToFind">The <see cref="Assembly"/> to find within the existing <see cref="Assemblies"/>.</param>
        /// <param name="assemblies">The assemblies to add</param>
        /// <remarks>Where a specified <see cref="Assembly"/> item already exists within the <see cref="Assemblies"/> it will not be added again.</remarks>
        public void AddAssemblyAfter(Assembly assemblyToFind, params Assembly[] assemblies) => AddAssemblyAfter(assemblyToFind, assemblies.Select(x => new MigrationAssemblyArgs(x)).ToArray());

        /// <summary>
        /// Adds one or more <paramref name="assemblies"/> to the <see cref="Assemblies"/> after the specified <paramref name="assemblyToFind"/>; where not found, will be added to the end.
        /// </summary>
        /// <param name="assemblyToFind">The <see cref="Assembly"/> to find within the existing <see cref="Assemblies"/>.</param>
        /// <param name="assemblies">The assemblies to add</param>
        /// <remarks>Where a specified <see cref="Assembly"/> item already exists within the <see cref="Assemblies"/> it will not be added again.</remarks>
        public void AddAssemblyAfter(Assembly assemblyToFind, params MigrationAssemblyArgs[] assemblies)
        {
            var index = _assemblies.FindIndex(x => x.Assembly == assemblyToFind.ThrowIfNull(nameof(assemblyToFind)));
            if (index < 0)
            {
                AddAssembly(assemblies);
                return;
            }

            var newAssemblies = new List<MigrationAssemblyArgs>();
            foreach (var assembly in assemblies)
            {
                if (!_assemblies.Any(x => x.Assembly == assembly.Assembly) && !newAssemblies.Any(x => x.Assembly == assembly.Assembly))
                    newAssemblies.Add(assembly);
            }

            _assemblies.InsertRange(index + 1, newAssemblies);
        }

        /// <summary>
        /// Adds a parameter to the <see cref="Parameters"/> where it does not already exist; unless <paramref name="overrideExisting"/> is selected then it will add or override.
        /// </summary>
        /// <param name="key">The parameter key.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="overrideExisting">Indicates whether to override the existing value where it is pre-existing; otherwise, will not add/update.</param>
        /// <returns>The current <see cref="MigrationArgs"/> instance to support fluent-style method-chaining.</returns>
        public void AddParameter(string key, object? value, bool overrideExisting = false)
        {
            if (!Parameters.TryAdd(key, value) && overrideExisting)
                Parameters[key] = value;
        }

        /// <summary>
        /// Copy and replace from <paramref name="args"/>.
        /// </summary>
        /// <param name="args">The <see cref="MigrationArgsBase{TSelf}"/> to copy from.</param>
        protected void CopyFrom(MigrationArgsBase args)
        {
            base.CopyFrom(args.ThrowIfNull(nameof(args)));

            MigrationCommand = args.MigrationCommand;
            _assemblies.Clear();
            _assemblies.AddRange(args.Assemblies);
            Parameters.Clear();
            args.Parameters.ForEach(x => Parameters.Add(x.Key, x.Value));
            Logger = args.Logger;
            OutputDirectory = args.OutputDirectory;
            SchemaOrder.Clear();
            SchemaOrder.AddRange(args.SchemaOrder);
            DataParserArgs.CopyFrom(args.DataParserArgs);
            IdColumnNameSuffix = args.IdColumnNameSuffix;
            CodeColumnNameSuffix = args.CodeColumnNameSuffix;
            CreatedDateColumnName = args.CreatedDateColumnName;
            CreatedByColumnName = args.CreatedByColumnName;
            UpdatedDateColumnName = args.UpdatedDateColumnName;
            UpdatedByColumnName = args.UpdatedByColumnName;
            RowVersionColumnName = args.RowVersionColumnName;
            TenantIdColumnName = args.TenantIdColumnName;
            RefDataCodeColumnName = args.RefDataCodeColumnName;
            RefDataTextColumnName = args.RefDataTextColumnName;
            EmitDotNetDateOnly = args.EmitDotNetDateOnly;
            EmitDotNetTimeOnly = args.EmitDotNetTimeOnly;
            DataResetFilterPredicate = args.DataResetFilterPredicate;

            if (args.ExecuteStatements == null)
                ExecuteStatements = null;
            else
            {
                ExecuteStatements = [];
                ExecuteStatements.AddRange(args.ExecuteStatements);
            }
        }

        /// <summary>
        /// Creates a copy of the <see cref="Parameters"/> where all <see cref="KeyValuePair{TKey, TValue}.Value"/> are converted to a <see cref="string"/> or <c>null</c>.
        /// </summary>
        public IDictionary<string, string?> CreateStringParameters()
        {
            var dict = new Dictionary<string, string?>();
            foreach (var item in Parameters)
            {
                if (item.Value == null)
                    dict.Add(item.Key, null);
                else
                    dict.Add(item.Key, item.Value.ToString());
            }

            return dict;
        }
    }
}