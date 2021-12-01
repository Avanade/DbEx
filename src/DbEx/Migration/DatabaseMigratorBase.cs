﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.Migration.Data;
using DbUp.Engine;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DbEx.Migration
{
    /// <summary>
    /// Represents the base capabilities for the database migration orchestrator leveraging <see href="https://dbup.readthedocs.io/en/latest/">DbUp</see>.
    /// </summary>
    public abstract class DatabaseMigratorBase
    {
        private const string NothingFoundText = "  ** Nothing found. **";

        /// <summary>
        /// Initializes an instance of the <see cref="DatabaseMigratorBase"/> class.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="command">The <see cref="MigrationCommand"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="assemblies">The <see cref="Assembly"/> list to use to probe for assembly resource (in specified sequence).</param>
        protected DatabaseMigratorBase(string connectionString, MigrationCommand command, ILogger logger, params Assembly[] assemblies)
        {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            Command = command;
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            LoggerSink = new LoggerSink(logger);

            Assemblies = new List<Assembly>(assemblies ?? Array.Empty<Assembly>());

            var list = new List<string>();
            foreach (var ass in Assemblies)
            {
                list.Add(ass.GetName().Name!);
            }

            Namespaces = list;
            ParserArgs = new DataParserArgs();
        }

        /// <summary>
        /// Gets the database connection string.
        /// </summary>
        protected string ConnectionString { get; }

        /// <summary>
        /// Gets the <see cref="MigrationCommand"/>.
        /// </summary>
        protected MigrationCommand Command { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        protected ILogger Logger { get;  }

        /// <summary>
        /// Gets the <see cref="Migration.LoggerSink"/>.
        /// </summary>
        protected LoggerSink LoggerSink { get; }

        /// <summary>
        /// Gets the <see cref="Assembly"/> list to use to probe for assembly resource (in specified sequence).
        /// </summary>
        protected IReadOnlyList<Assembly> Assemblies { get; }

        /// <summary>
        /// Gets the root namespaces for the <see cref="Assemblies"/>.
        /// </summary>
        protected IEnumerable<string> Namespaces { get; }

        /// <summary>
        /// Gets the schema priority list (used to specify schema precedence; otherwise equal last).
        /// </summary>
        protected List<string> SchemaOrder { get; } = new List<string>();

        /// <summary>
        /// Gets the output <see cref="DirectoryInfo"/> where <see cref="MigrationCommand.Schema"/> objects may be found (these will take precedence over same named embedded resources).
        /// </summary>
        protected DirectoryInfo? OutputDirectory { get; set; }

        /// <summary>
        /// Gets or sets the <b>Migrations</b> scripts namespace part name.
        /// </summary>
        public string MigrationsNamespace { get; set; } = "Migrations";

        /// <summary>
        /// Gets or sets the <b>Migrations</b> scripts namespace part name.
        /// </summary>
        public string SchemaNamespace { get; set; } = "Schema";

        /// <summary>
        /// Gets or sets the <b>Migrations</b> scripts namespace part name.
        /// </summary>
        public string DataNamespace { get; set; } = "Data";

        /// <summary>
        /// Gets the list of known schema object types in the order of precedence.
        /// </summary>
        /// <remarks>The objects will be added in the order specified, and removed in the reverse order. This is to allow for potential dependencies between the object types.</remarks>
        protected abstract string[] KnownSchemaObjectTypes { get; }

        /// <summary>
        /// Gets or sets the <see cref="DataParserArgs"/>.
        /// </summary>
        /// <remarks>This is used by <see cref="MigrationCommand.Data"/> only; specifically by the <see cref="DataParser"/>.</remarks>
        public DataParserArgs ParserArgs { get; set; }

        /// <summary>
        /// Orchestrates the migration steps as specified by the <see cref="MigrationCommand"/>.
        /// </summary>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        public async Task<bool> MigrateAsync()
        {
            // Database drop.
            if (!await CommandExecuteAsync(MigrationCommand.Drop, "DATABASE DROP: Checking database existence and dropping where found...", async () => await DatabaseDropAsync().ConfigureAwait(false)))
                return false;

            // Database create.
            if (!await CommandExecuteAsync(MigrationCommand.Create, "DATABASE CREATE: Checking database existence and creating where not found...", async () => await DatabaseCreateAsync().ConfigureAwait(false)))
                return false;

            // Database migration scripts.
            if (!await CommandExecuteAsync(MigrationCommand.Migrate, "DATABASE MIGRATE: Migrating the database...", async () => await DatabaseMigrateAsync().ConfigureAwait(false)))
                return false;

            // Database schema scripts.
            if (!await CommandExecuteAsync(MigrationCommand.Schema, "DATABASE SCHEMA: Drops and creates the database objects...", async () => await DatabaseSchemaAsync().ConfigureAwait(false)))
                return false;

            // Database reset.
            if (!await CommandExecuteAsync(MigrationCommand.Reset, "DATABASE RESET: Resets database by dropping data from all tables...", async () => await DatabaseResetAsync().ConfigureAwait(false)))
                return false;

            // Database data load.
            if (!await CommandExecuteAsync(MigrationCommand.Data, "DATABASE DATA: Insert or merge the embedded YAML data...", async () => await DatabaseDataAsync().ConfigureAwait(false)))
                return false;

            return true;
        }

        /// <summary>
        /// Verifies execution, then wraps and times the command execution.
        /// </summary>
        private async Task<bool> CommandExecuteAsync(MigrationCommand command, string title, Func<Task<bool>> action, Func<string>? summary = null)
        {
            var isSelected = Command.HasFlag(command);

            if (!await OnBeforeCommandAsync(command, isSelected).ConfigureAwait(false))
                return false;

            if (isSelected)
            {
                if (!await CommandExecuteAsync(title, action, summary).ConfigureAwait(false))
                    return false;
            }

            return await OnAfterCommandAsync(command, isSelected).ConfigureAwait(false);
        }

        /// <summary>
        /// Provides an opportunity to perform additional processing <i>before</i> the <paramref name="command"/> is executed.
        /// </summary>
        /// <param name="command">The <see cref="MigrationCommand"/>.</param>
        /// <param name="isSelected">Indicates whether the <paramref name="command"/> is selected (see <see cref="Command"/>).</param>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        /// <remarks>This will be invoked for a command even where not selected for execution.</remarks>
        protected virtual Task<bool> OnBeforeCommandAsync(MigrationCommand command, bool isSelected) => Task.FromResult(true);

        /// <summary>
        /// Provides an opportunity to perform additional processing <i>after</i> the <paramref name="command"/> is executed.
        /// </summary>
        /// <param name="command">The <see cref="MigrationCommand"/>.</param>
        /// <param name="isSelected">Indicates whether the <paramref name="command"/> is selected (see <see cref="Command"/>).</param>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        /// <remarks>This will be invoked for a command even where not selected for execution, or unless the command execution failed.</remarks>
        protected virtual Task<bool> OnAfterCommandAsync(MigrationCommand command, bool isSelected) => Task.FromResult(true);

        /// <summary>
        /// Wraps and times the command execution.
        /// </summary>
        /// <param name="title">The title text.</param>
        /// <param name="action">The primary action to be performed.</param>
        /// <param name="summary">Optional summary text appended to the complete log text.</param>
        /// <remarks>This will also catch any unhandled exceptions and log accordingly.</remarks>
        protected async Task<bool> CommandExecuteAsync(string title, Func<Task<bool>> action, Func<string>? summary = null)
        {
            Logger.LogInformation(string.Empty);
            Logger.LogInformation(new string('-', 80));
            Logger.LogInformation(string.Empty);
            Logger.LogInformation(title ?? throw new ArgumentNullException(nameof(title)));

            try
            {
                var sw = Stopwatch.StartNew();
                var result = await (action ?? throw new ArgumentNullException(nameof(action))).Invoke().ConfigureAwait(false);
                sw.Stop();

                Logger.LogInformation($"Complete [{sw.ElapsedMilliseconds}ms{summary?.Invoke() ?? string.Empty}].");
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Performs the script deployment.
        /// </summary>
        /// <param name="scripts">The <see cref="SqlScript"/> list.</param>
        /// <returns>The <see cref="DatabaseUpgradeResult"/>.</returns>
        protected abstract Task<DatabaseUpgradeResult> DeployChangesAsync(IEnumerable<SqlScript> scripts);

        /// <summary>
        /// Performs the <see cref="MigrationCommand.Drop"/> command.
        /// </summary>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        /// <remarks>This is invoked by using the <see cref="CommandExecuteAsync(string, Func{Task{bool}}, Func{string}?)"/>.</remarks>
        protected abstract Task<bool> DatabaseDropAsync();

        /// <summary>
        /// Performs the <see cref="MigrationCommand.Create"/> command.
        /// </summary>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        /// <remarks>This is invoked by using the <see cref="CommandExecuteAsync(string, Func{Task{bool}}, Func{string}?)"/>.</remarks>
        protected abstract Task<bool> DatabaseCreateAsync();

        /// <summary>
        /// Performs the <see cref="MigrationCommand.Migrate"/> command.
        /// </summary>
        private async Task<bool> DatabaseMigrateAsync()
        {
            Logger.LogInformation($"  Probing for embedded resources: {string.Join(", ", GetNamespacesWithSuffix($"{MigrationsNamespace}.*.sql"))}");

            var scripts = new List<SqlScript>();
            foreach (var ass in Assemblies)
            {
                foreach (var name in ass.GetManifestResourceNames().Where(rn => Namespaces.Any(ns => rn.StartsWith($"{ns}.{MigrationsNamespace}.", StringComparison.InvariantCulture))))
                {
                    // Determine run order and add script to list.
                    var order = name.EndsWith(".pre.deploy.sql", StringComparison.InvariantCultureIgnoreCase) ? 1 :
                                name.EndsWith(".post.deploy.sql", StringComparison.InvariantCultureIgnoreCase) ? 3 : 2;

                    scripts.Add(SqlScript.FromStream(name, ass.GetManifestResourceStream(name), Encoding.Default, new SqlScriptOptions { RunGroupOrder = order, ScriptType = DbUp.Support.ScriptType.RunOnce }));
                }
            }

            if (scripts.Count == 0)
            {
                Logger.LogInformation(NothingFoundText);
                return true;
            }

            Logger.LogInformation("  Migrate (using DbUp) the embedded resources...");
            return (await DeployChangesAsync(scripts).ConfigureAwait(false)).Successful;
        }

        /// <summary>
        /// Performs the <see cref="MigrationCommand.Schema"/> command.
        /// </summary>
        private async Task<bool> DatabaseSchemaAsync()
        {
            // Build list of all known schema type objects to be dropped and created.
            var scripts = new List<DatabaseMigrationScript>();

            // See if there are any files out there that should take precedence over embedded resources.
            if (OutputDirectory != null)
            {
                var di = new DirectoryInfo(Path.Combine(OutputDirectory.FullName, SchemaNamespace));
                Logger.LogInformation($"  Probing for files (recursively): {Path.Combine(di.FullName, "*", "*.sql")}");

                if (di.Exists)
                {
                    foreach (var fi in di.GetFiles("*.sql", SearchOption.AllDirectories))
                    {
                        var dir = fi.DirectoryName[(OutputDirectory!.FullName.Length + 1)..];
                        var file = fi.Name[..(fi.Name.Length - fi.Extension.Length)];
                        var rn = $"{dir}.{file}{fi.Extension}".Replace(' ', '_').Replace('-', '_').Replace('\\', '.').Replace('/', '.');
                        scripts.Add(new DatabaseMigrationScript(fi, rn));
                    }
                }
            }

            // Get all the resources from the assemblies.
            Logger.LogInformation($"  Probing for embedded resources: {string.Join(", ", GetNamespacesWithSuffix($"{SchemaNamespace}.*.sql"))}");
            foreach (var ass in Assemblies)
            {
                foreach (var rn in ass.GetManifestResourceNames())
                {
                    // Filter on schema namespace prefix and suffix of '.sql'.
                    if (!(Namespaces.Any(x => rn.StartsWith($"{x}.{SchemaNamespace}.", StringComparison.InvariantCulture) && rn.EndsWith(".sql", StringComparison.InvariantCultureIgnoreCase))))
                        continue;

                    // Filter out any picked up from file system probe above.
                    if (scripts.Any(x => x.Name == rn))
                        continue;

                    scripts.Add(new DatabaseMigrationScript(ass, rn));
                }
            }

            // Make sure there is work to be done.
            if (scripts.Count == 0)
            {
                Logger.LogInformation(NothingFoundText);
                return true;
            }

            // Execute the database specific logic.
            return await DatabaseSchemaAsync(scripts).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs the <see cref="MigrationCommand.Schema"/> command.
        /// </summary>
        /// <param name="scripts">The <see cref="DatabaseMigrationScript"/> list discovered during the file and resource probes.</param>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        /// <remarks>This is invoked by using the <see cref="CommandExecuteAsync(string, Func{Task{bool}}, Func{string}?)"/>.</remarks>
        protected abstract Task<bool> DatabaseSchemaAsync(List<DatabaseMigrationScript> scripts);

        /// <summary>
        /// Performs the <see cref="MigrationCommand.Reset"/> command.
        /// </summary>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        /// <remarks>This is invoked by using the <see cref="CommandExecuteAsync(string, Func{Task{bool}}, Func{string}?)"/>.</remarks>
        protected abstract Task<bool> DatabaseResetAsync();

        /// <summary>
        /// Performs the <see cref="MigrationCommand.Data"/> command.
        /// </summary>
        private async Task<bool> DatabaseDataAsync()
        {
            Logger.LogInformation($"  Probing for embedded resources: {string.Join(", ", GetNamespacesWithSuffix($"{DataNamespace}.*.sql", true))}");

            var list = new List<(Assembly Assembly, string ResourceName)>();
            foreach (var ass in Assemblies.Reverse())
            {
                foreach (var rn in ass.GetManifestResourceNames())
                {
                    // Filter on schema namespace prefix and suffix of '.sql'.
                    if (!Namespaces.Any(x => rn.StartsWith($"{x}.{DataNamespace}.", StringComparison.InvariantCulture) && (rn.EndsWith(".yaml", StringComparison.InvariantCultureIgnoreCase) || rn.EndsWith(".yml", StringComparison.InvariantCultureIgnoreCase))))
                        continue;

                    list.Add((ass, rn));
                }
            }

            // Make sure there is work to be done.
            if (list.Count == 0)
            {
                Logger.LogInformation(NothingFoundText);
                return true;
            }

            // Infer database schema.
            Logger.LogInformation("  Querying database to infer table(s)/column(s) schema...");
            var db = CreateDatabase(ConnectionString) ?? throw new InvalidOperationException($"An {nameof(IDatabase)} instance must be returned from the {nameof(CreateDatabase)} method.");
            var pargs = ParserArgs ?? new DataParserArgs();
            var dbTables = await db.SelectSchemaAsync(pargs.RefDataPredicate).ConfigureAwait(false);

            // Iterate through each resource - parse the data, then insert/merge as requested.
            var parser = new DataParser(dbTables, pargs);
            foreach (var item in list)
            {
                try
                {
                    Logger.LogInformation(string.Empty);
                    Logger.LogInformation($"** Parsing and executing: {item.ResourceName}");
                    using var sr = new StreamReader(item.Assembly.GetManifestResourceStream(item.ResourceName)!);

                    var tables = await parser.ParseYamlAsync(sr);

                    if (!await DatabaseDataAsync(db, tables).ConfigureAwait(false))
                        return false;
                }
                catch (DataParserException dpex)
                {
                    Logger.LogError(dpex.Message);
                    return false;
                }
            }

            // All good if we got this far!
            return true;
        }

        /// <summary>
        /// Performs the <see cref="MigrationCommand.Data"/> command.
        /// </summary>
        /// <param name="database">The <see cref="IDatabase"/>.</param>
        /// <param name="dataTables">The <see cref="DataTable"/> list that contains the parsed data to be inserted/merged.</param>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        /// <remarks>This is invoked by using the <see cref="CommandExecuteAsync(string, Func{Task{bool}}, Func{string}?)"/>.</remarks>
        protected abstract Task<bool> DatabaseDataAsync(IDatabase database, List<DataTable> dataTables);

        /// <summary>
        /// Create an <see cref="IDatabase"/> instance.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        /// <returns>The <see cref="IDatabase"/> instance.</returns>
        protected abstract IDatabase CreateDatabase(string connectionString);

        /// <summary>
        /// Gets the <see cref="Namespaces"/> with the specified namespace suffix applied.
        /// </summary>
        private IEnumerable<string> GetNamespacesWithSuffix(string suffix, bool reverse = false)
        {
            if (suffix == null)
                throw new ArgumentNullException(nameof(suffix));

            var list = new List<string>();
            foreach (var ns in reverse ? Namespaces.Reverse() : Namespaces)
            {
                list.Add($"{ns}.{suffix}");
            }

            return list.Count == 0 ? new string[] { "(none)" } : list;
        }
    }
}