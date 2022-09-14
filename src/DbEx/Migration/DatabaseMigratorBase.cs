// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.Database;
using DbEx.Migration.Data;
using Microsoft.Extensions.Logging;
using OnRamp.Console;
using OnRamp.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
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
            Assemblies = new List<Assembly>(assemblies ?? Array.Empty<Assembly>());

            var list = new List<string>();
            foreach (var ass in Assemblies)
            {
                list.Add(ass.GetName().Name!);
            }

            OutputDirectory = new DirectoryInfo(CodeGenConsole.GetBaseExeDirectory());
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
        /// Gets the <see cref="IDatabase"/>.
        /// </summary>
        protected abstract IDatabase Database { get; }

        /// <summary>
        /// Gets the <see cref="IDatabaseJournal"/>.
        /// </summary>
        protected abstract IDatabaseJournal Journal { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        protected ILogger Logger { get;  }

        /// <summary>
        /// Gets the <see cref="Assembly"/> list to use to probe for assembly resource (in specified sequence).
        /// </summary>
        protected List<Assembly> Assemblies { get; }

        /// <summary>
        /// Gets the root namespaces for the <see cref="Assemblies"/>.
        /// </summary>
        protected IEnumerable<string> Namespaces { get; }

        /// <summary>
        /// Gets the schema priority list (used to specify schema precedence; otherwise equal last).
        /// </summary>
        protected List<string> SchemaOrder { get; } = new List<string>();

        /// <summary>
        /// Gets the output parent <see cref="DirectoryInfo"/> where <see cref="MigrationCommand.Schema"/> and <see cref="CreateScriptAsync"/> artefacts reside.
        /// </summary>
        protected DirectoryInfo OutputDirectory { get; set; }

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
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        public virtual async Task<bool> MigrateAsync(CancellationToken cancellationToken = default)
        {
            // Check commands.
            if (Command.HasFlag(MigrationCommand.Execute))
                throw new InvalidOperationException($@"{nameof(MigrateAsync)} does not support {nameof(MigrationCommand)}.{nameof(MigrationCommand.Execute)}, please invoke {nameof(ExecuteSqlStatementsAsync)} method directly.");

            if (Command.HasFlag(MigrationCommand.Script))
                throw new InvalidOperationException($@"{nameof(MigrateAsync)} does not support {nameof(MigrationCommand)}.{nameof(MigrationCommand.Script)}, please invoke {nameof(CreateScriptAsync)} method directly.");

            // Database drop.
            if (!await CommandExecuteAsync(MigrationCommand.Drop, "DATABASE DROP: Checking database existence and dropping where found...", DatabaseDropAsync, null, cancellationToken).ConfigureAwait(false))
                return false;

            // Database create.
            if (!await CommandExecuteAsync(MigrationCommand.Create, "DATABASE CREATE: Checking database existence and creating where not found...", DatabaseCreateAsync, null, cancellationToken).ConfigureAwait(false))
                return false;

            // Database migration scripts.
            if (!await CommandExecuteAsync(MigrationCommand.Migrate, "DATABASE MIGRATE: Migrating the database...", DatabaseMigrateAsync, null, cancellationToken).ConfigureAwait(false))
                return false;

            // Database schema scripts.
            if (!await CommandExecuteAsync(MigrationCommand.Schema, "DATABASE SCHEMA: Drops and creates the database objects...", DatabaseSchemaAsync, null, cancellationToken).ConfigureAwait(false))
                return false;

            // Database reset.
            if (!await CommandExecuteAsync(MigrationCommand.Reset, "DATABASE RESET: Resets database by dropping data from all tables...", DatabaseResetAsync, null, cancellationToken).ConfigureAwait(false))
                return false;

            // Database data load.
            if (!await CommandExecuteAsync(MigrationCommand.Data, "DATABASE DATA: Insert or merge the embedded YAML data...", DatabaseDataAsync, null, cancellationToken).ConfigureAwait(false))
                return false;

            return true;
        }

        /// <summary>
        /// Verifies execution, then wraps and times the command execution.
        /// </summary>
        private async Task<bool> CommandExecuteAsync(MigrationCommand command, string title, Func<CancellationToken, Task<bool>> action, Func<string>? summary, CancellationToken cancellationToken)
        {
            var isSelected = Command.HasFlag(command);

            if (!await OnBeforeCommandAsync(command, isSelected).ConfigureAwait(false))
                return false;

            if (isSelected)
            {
                if (!await CommandExecuteAsync(title, action, summary, cancellationToken).ConfigureAwait(false))
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
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>This will also catch any unhandled exceptions and log accordingly.</remarks>
        protected async Task<bool> CommandExecuteAsync(string title, Func<CancellationToken, Task<bool>> action, Func<string>? summary, CancellationToken cancellationToken)
        {
            Logger.LogInformation("{Content}", string.Empty);
            Logger.LogInformation("{Content}", new string('-', 80));
            Logger.LogInformation("{Content}", string.Empty);
            Logger.LogInformation("{Content}", title ?? throw new ArgumentNullException(nameof(title)));

            try
            {
                var sw = Stopwatch.StartNew();
                if (!await (action ?? throw new ArgumentNullException(nameof(action))).Invoke(cancellationToken).ConfigureAwait(false))
                    return false;

                sw.Stop();
                Logger.LogInformation("{Content}", string.Empty);
                Logger.LogInformation("{Content}", $"Complete. [{sw.Elapsed.TotalMilliseconds}ms{summary?.Invoke() ?? string.Empty}]");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{Content}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Execute the <paramref name="scripts"/>.
        /// </summary>
        /// <param name="scripts">The <see cref="DatabaseMigrationScript"/> list.</param>
        /// <param name="includeExecutionLogging">Indicates whether to include detailed execution logging.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        public abstract Task<bool> ExecuteScriptsAsync(IEnumerable<DatabaseMigrationScript> scripts, bool includeExecutionLogging, CancellationToken cancellationToken);

        /// <summary>
        /// Performs the <see cref="MigrationCommand.Drop"/> command.
        /// </summary>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>This is invoked by using the <see cref="CommandExecuteAsync(string, Func{CancellationToken, Task{bool}}, Func{string}?, CancellationToken)"/>.</remarks>
        protected abstract Task<bool> DatabaseDropAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs the <see cref="MigrationCommand.Create"/> command.
        /// </summary>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>This is invoked by using the <see cref="CommandExecuteAsync(string, Func{CancellationToken, Task{bool}}, Func{string}?, CancellationToken)"/>.</remarks>
        protected abstract Task<bool> DatabaseCreateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs the <see cref="MigrationCommand.Migrate"/> command.
        /// </summary>
        private async Task<bool> DatabaseMigrateAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("{Content}", $"  Probing for embedded resources: {string.Join(", ", GetNamespacesWithSuffix($"{MigrationsNamespace}.*.sql"))}");

            var scripts = new List<DatabaseMigrationScript>();
            foreach (var ass in Assemblies)
            {
                foreach (var name in ass.GetManifestResourceNames().Where(rn => Namespaces.Any(ns => rn.StartsWith($"{ns}.{MigrationsNamespace}.", StringComparison.InvariantCulture))).OrderBy(x => x))
                {
                    // Determine run order and add script to list.
                    var order = name.EndsWith(".pre.deploy.sql", StringComparison.InvariantCultureIgnoreCase) ? 1 :
                                name.EndsWith(".post.deploy.sql", StringComparison.InvariantCultureIgnoreCase) ? 3 : 2;

                    using var sr = new StreamReader(ass.GetManifestResourceStream(name)!);
                    scripts.Add(new DatabaseMigrationScript(ass, name) { GroupOrder = order, RunAlways = order != 2 });
                }
            }

            if (scripts.Count == 0)
            {
                Logger.LogInformation("{Content}", NothingFoundText);
                return true;
            }

            Logger.LogInformation("{Content}", "  Migrate the embedded resources...");
            return await ExecuteScriptsAsync(scripts, true, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs the <see cref="MigrationCommand.Schema"/> command.
        /// </summary>
        private async Task<bool> DatabaseSchemaAsync(CancellationToken cancellationToken)
        {
            // Build list of all known schema type objects to be dropped and created.
            var scripts = new List<DatabaseMigrationScript>();

            // See if there are any files out there that should take precedence over embedded resources.
            if (OutputDirectory != null)
            {
                var di = new DirectoryInfo(Path.Combine(OutputDirectory.FullName, SchemaNamespace));
                Logger.LogInformation("{Content}", $"  Probing for files (recursively): {Path.Combine(di.FullName, "*", "*.sql")}");

                if (di.Exists)
                {
                    foreach (var fi in di.GetFiles("*.sql", SearchOption.AllDirectories))
                    {
                        var rn = $"{fi.FullName[(OutputDirectory.Parent.FullName.Length + 1)..]}".Replace(' ', '_').Replace('-', '_').Replace('\\', '.').Replace('/', '.');
                        scripts.Add(new DatabaseMigrationScript(fi, rn));
                    }
                }
            }

            // Get all the resources from the assemblies.
            Logger.LogInformation("{Content}", $"  Probing for embedded resources: {string.Join(", ", GetNamespacesWithSuffix($"{SchemaNamespace}.*.sql"))}");
            foreach (var ass in Assemblies)
            {
                foreach (var rn in ass.GetManifestResourceNames().OrderBy(x => x))
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
            return await DatabaseSchemaAsync(scripts, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs the <see cref="MigrationCommand.Schema"/> command.
        /// </summary>
        /// <param name="scripts">The <see cref="DatabaseMigrationScript"/> list discovered during the file and resource probes.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        /// <remarks>This is invoked by using the <see cref="CommandExecuteAsync(string, Func{CancellationToken, Task{bool}}, Func{string}?, CancellationToken)"/>.</remarks>
        protected abstract Task<bool> DatabaseSchemaAsync(List<DatabaseMigrationScript> scripts, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs the <see cref="MigrationCommand.Reset"/> command.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        /// <remarks>This is invoked by using the <see cref="CommandExecuteAsync(string, Func{CancellationToken, Task{bool}}, Func{string}?, CancellationToken)"/>.</remarks>
        protected abstract Task<bool> DatabaseResetAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs the <see cref="MigrationCommand.Data"/> command.
        /// </summary>
        private async Task<bool> DatabaseDataAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("{Content}", $"  Probing for embedded resources: {string.Join(", ", GetNamespacesWithSuffix($"{DataNamespace}.*.[sql|yaml]", true))}");

            var list = new List<(Assembly Assembly, string ResourceName)>();
            foreach (var ass in Assemblies)
            {
                foreach (var rn in ass.GetManifestResourceNames().OrderBy(x => x))
                {
                    // Filter on schema namespace prefix and suffix of '.sql'.
                    if (!Namespaces.Any(x => rn.StartsWith($"{x}.{DataNamespace}.", StringComparison.InvariantCulture) && (rn.EndsWith(".sql", StringComparison.InvariantCultureIgnoreCase) || rn.EndsWith(".yaml", StringComparison.InvariantCultureIgnoreCase) || rn.EndsWith(".yml", StringComparison.InvariantCultureIgnoreCase))))
                        continue;

                    list.Add((ass, rn));
                }
            }

            // Make sure there is work to be done.
            if (list.Count == 0)
            {
                Logger.LogInformation("{Content}", NothingFoundText);
                return true;
            }

            // Infer database schema.
            Logger.LogInformation("  Querying database to infer table(s)/column(s) schema...");
            var pargs = ParserArgs ?? new DataParserArgs();
            var dbTables = await Database.SelectSchemaAsync(pargs.RefDataPredicate).ConfigureAwait(false);

            // Iterate through each resource - parse the data, then insert/merge as requested.
            var parser = new DataParser(dbTables, pargs);
            foreach (var item in list)
            {
                using var sr = new StreamReader(item.Assembly.GetManifestResourceStream(item.ResourceName)!);

                if (item.ResourceName.EndsWith(".sql", StringComparison.InvariantCultureIgnoreCase))
                {
                    // Execute the SQL script directly.
                    Logger.LogInformation("{Content}", string.Empty);
                    Logger.LogInformation("{Content}", $"** Executing: {item.ResourceName}");

                    var ss = new DatabaseMigrationScript(item.Assembly, item.ResourceName) { RunAlways = true };
                    if (!await ExecuteScriptsAsync(new DatabaseMigrationScript[] { ss }, false, cancellationToken).ConfigureAwait(false))
                        return false;
                }
                else
                {
                    // Handle the YAML - parse and execute.
                    try
                    {
                        Logger.LogInformation("{Content}", string.Empty);
                        Logger.LogInformation("{Content}", $"** Parsing and executing: {item.ResourceName}");

                        var tables = await parser.ParseYamlAsync(sr);

                        if (!await DatabaseDataAsync(tables, cancellationToken).ConfigureAwait(false))
                            return false;
                    }
                    catch (DataParserException dpex)
                    {
                        Logger.LogError("{Content}", dpex.Message);
                        return false;
                    }
                }
            }

            // All good if we got this far!
            return true;
        }

        /// <summary>
        /// Performs the <see cref="MigrationCommand.Data"/> command.
        /// </summary>
        /// <param name="dataTables">The <see cref="DataTable"/> list that contains the parsed data to be inserted/merged.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        /// <remarks>This is invoked by using the <see cref="CommandExecuteAsync(string, Func{CancellationToken, Task{bool}}, Func{string}?, CancellationToken)"/>.</remarks>
        protected abstract Task<bool> DatabaseDataAsync(List<DataTable> dataTables, CancellationToken cancellationToken = default);

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

            return list.Count == 0 ? new string[] { "(none)" } : list.ToArray();
        }

        /// <summary>
        /// Creates a new script using the <paramref name="resourceName"/> template within the <see cref="MigrationsNamespace"/> folder.
        /// </summary>
        /// <param name="resourceName">The script resource template name; defaults to '<c>default</c>'.</param>
        /// <param name="parameters">The optional parameters.</param>
        /// <param name="extensions">The optional file extensions used to probe for resource; defaults to '<c>_sql.hb</c>' and '<c>_sql.hbs</c>'.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        public async Task<bool> CreateScriptAsync(string? resourceName = null, IDictionary<string, string?>? parameters = null, string[]? extensions = null, CancellationToken cancellationToken = default)
            => await CommandExecuteAsync("DATABASE SCRIPT: Create a new database script...", async ct => await CreateScriptInternalAsync(resourceName, parameters, extensions, ct).ConfigureAwait(false), null, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Creates the new script.
        /// </summary>
        private async Task<bool> CreateScriptInternalAsync(string? resourceName, IDictionary<string, string?>? parameters, string[]? extensions, CancellationToken cancellationToken)
        {
            resourceName ??= "default";
            if (extensions == null || extensions.Length == 0)
                extensions = new string[] { "_sql.hb", "_sql.hbs" };

            // Find the resource.
            var ass = Assemblies.Concat(new Assembly[] { typeof(DatabaseMigratorBase).Assembly }).ToArray();
            var sr = StreamLocator.GetResourcesStreamReader(resourceName, ass).StreamReader;
            foreach (var ext in extensions)
            {
                if (sr != null)
                    break;

                sr = StreamLocator.GetResourcesStreamReader(resourceName + ext, ass).StreamReader;
            }

            if (sr == null)
            {
                Logger.LogError("{Content}", $"The Script resource '{resourceName}' does not exist.");
                return false;
            }

            // Read the resource.
            using var usr = sr;
            var txt = await usr.ReadToEndAsync().ConfigureAwait(false);

            // Extract the filename from content if specified.
            var data = new { Parameters = parameters ?? new Dictionary<string, string?>() };
            var lines = txt.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            string fn = "new-script";
            foreach (var line in lines)
            {
                var lt = line.Trim();
                if (lt.StartsWith("{{! FILENAME:", StringComparison.InvariantCultureIgnoreCase) && lt.EndsWith("}}", StringComparison.InvariantCultureIgnoreCase))
                {
                    fn = lt[13..^2].Trim();
                    continue;
                }

                if (lt.StartsWith("{{! PARAM:", StringComparison.InvariantCultureIgnoreCase) && lt.EndsWith("}}", StringComparison.InvariantCultureIgnoreCase))
                {
                    var pv = lt[10..^2].Trim();
                    if (string.IsNullOrEmpty(pv))
                        continue;

                    var parts = pv.Split('=', StringSplitOptions.RemoveEmptyEntries);
                    data.Parameters.TryAdd(parts[0], parts.Length <= 1 ? null : parts[1].Trim());
                }
            }

            // Update the filename.
            fn = $"{DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture)}-{fn.Replace("[", "{{").Replace("]", "}}")}.sql";
            fn = Path.Combine(OutputDirectory.FullName, MigrationsNamespace, new HandlebarsCodeGenerator(fn).Generate(data).Replace(" ", "-").ToLowerInvariant());
            var fi = new FileInfo(fn);

            // Generate the script content and write to file system.
            if (!fi.Directory.Exists)
                fi.Directory.Create();

            await File.WriteAllTextAsync(fi.FullName, new HandlebarsCodeGenerator(txt).Generate(data), cancellationToken).ConfigureAwait(false);

            Logger.LogWarning("{Content}", $"Script file created: {fi.FullName}");
            return true;
        }

        /// <summary>
        /// Executes the raw SQL statements by creating the equivalent <see cref="DatabaseMigrationScript"/> and invoking <see cref="ExecuteScriptsAsync"/>.
        /// </summary>
        /// <param name="statements">The SQL statements.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        /// <remarks>A maximum of 999 SQL statements may be executed at one-time. Each script is run independently (i.e. not within an overall database tramsaction); therefore, any preceeding scripts before error will have executed successfully.</remarks>
        public async Task<bool> ExecuteSqlStatementsAsync(string[]? statements, CancellationToken cancellationToken = default)
            => await CommandExecuteAsync("DATABASE EXECUTE: Executes the SQL statement(s)...", async ct => await ExecuteSqlStatementsInternalAsync(statements, ct).ConfigureAwait(false), null, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Executes the raw SQL statements.
        /// </summary>
        private async Task<bool> ExecuteSqlStatementsInternalAsync(string[]? statements, CancellationToken cancellationToken)
        {
            if (statements == null || statements.Length == 0)
            {
                Logger.LogInformation("  No statements to execute.");
                return true;
            }

            if (statements.Length >= 1000)
                throw new ArgumentException("A maximum of 999 SQL statements may be executed at one-time.", nameof(statements));

            var sn = $"{DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture)}-console-execute-";

            var scripts = new List<DatabaseMigrationScript>();
            for (int i = 0; i < statements.Length; i++)
            {
                if (File.Exists(statements[i]))
                    scripts.Add(new DatabaseMigrationScript(new FileInfo(statements[i]), statements[i]));
                else
                    scripts.Add(new DatabaseMigrationScript(statements[i], $"{sn}{i + 1:000}.sql"));
            }

            return await ExecuteScriptsAsync(scripts, false, cancellationToken).ConfigureAwait(false);
        }
    }
}