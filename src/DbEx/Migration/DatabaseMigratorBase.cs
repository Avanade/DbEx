// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.Database;
using DbEx.Console;
using DbEx.Migration.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    /// Represents the base capabilities for the database migration orchestrator leveraging <see href="https://dbup.readthedocs.io/en/latest/">DbUp</see> (where applicable).
    /// </summary>
    public abstract class DatabaseMigratorBase
    {
        private const string NothingFoundText = "  ** Nothing found. **";
        private HandlebarsCodeGenerator? _dataCodeGen;

        /// <summary>
        /// Initializes an instance of the <see cref="DatabaseMigratorBase"/> class.
        /// </summary>
        /// <param name="args">The <see cref="MigratorConsoleArgsBase"/>.</param>
        protected DatabaseMigratorBase(MigratorConsoleArgsBase args)
        {
            Args = args ?? throw new ArgumentNullException(nameof(args));
            if (string.IsNullOrEmpty(Args.ConnectionString))
                throw new ArgumentException($"{nameof(MigratorConsoleArgs.ConnectionString)} property must have a value.", nameof(args));

            Args.Logger ??= NullLogger.Instance;
            Args.OutputDirectory ??= new DirectoryInfo(CodeGenConsole.GetBaseExeDirectory());

            var list = new List<string>();
            foreach (var ass in Args.Assemblies)
            {
                list.Add(ass.GetName().Name!);
            }

            Namespaces = list;
            ArtefactResourceAssemblies = Args.Assemblies.Concat(new Assembly[] { GetType().Assembly, typeof(DatabaseMigratorBase).Assembly }).Distinct().ToArray();
            SchemaObjectTypes = Array.Empty<string>();

            Journal = new DatabaseJournal(this);
        }

        /// <summary>
        /// Gets the <see cref="MigratorConsoleArgsBase"/>.
        /// </summary>
        public MigratorConsoleArgsBase Args { get; }

        /// <summary>
        /// Gets the database provider name.
        /// </summary>
        /// <remarks>Used as the prefix for embedded resources.</remarks>
        public abstract string Provider { get; }

        /// <summary>
        /// Gets the database name.
        /// </summary>
        /// <remarks>This should be inferred from the <see cref="Args"/> <see cref="OnRamp.CodeGeneratorDbArgsBase.ConnectionString"/>.</remarks>
        public abstract string DatabaseName { get; }

        /// <summary>
        /// Gets the <see cref="IDatabase"/>.
        /// </summary>
        public abstract IDatabase Database { get; }

        /// <summary>
        /// Gets the 'master' <see cref="IDatabase"/>.
        /// </summary>
        /// <remarks>Returns the <see cref="Database"/> by default (unless specifically overridden).</remarks>
        public virtual IDatabase MasterDatabase => Database;

        /// <summary>
        /// Gets the <see cref="IDatabaseJournal"/>.
        /// </summary>
        public virtual IDatabaseJournal Journal { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/> (references <see cref="MigratorConsoleArgsBase.Logger"/>).
        /// </summary>
        public ILogger Logger => Args.Logger!;

        /// <summary>
        /// Gets the root namespaces for the <see cref="MigratorConsoleArgsBase.Assemblies"/>.
        /// </summary>
        protected IEnumerable<string> Namespaces { get; }

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
        /// Gets or sets the list of supported schema object types in the order of precedence.
        /// </summary>
        /// <remarks>The objects will be added in the order specified, and removed in the reverse order. This is to allow for potential dependencies between the object types.
        /// <para>Where none are specified then the <see cref="MigrationCommand.Schema"/> phase will be skipped.</para></remarks>
        public string[] SchemaObjectTypes { get; set; }

        /// <summary>
        /// Gets the assemblies used for probing the requisite artefact resources (used for providing the underlying requisite database statements for the specified <see cref="Provider"/>).
        /// </summary>
        /// <remarks>Uses the <see cref="MigratorConsoleArgsBase.Assemblies"/> as the base, then adds for <c>this</c> <see cref="Type"/>.</remarks>
        public Assembly[] ArtefactResourceAssemblies { get; }

        /// <summary>
        /// Indicates whether <see cref="MigrationCommand.CodeGen"/> functionality is enabled.
        /// </summary>
        /// <remarks>Where supported the <see cref="DatabaseCodeGenAsync(CancellationToken)"/> will be invoked and must be overridden to implement.</remarks>
        public bool IsCodeGenEnabled { get; protected set; }

        /// <summary>
        /// Orchestrates the migration steps as specified by the <see cref="MigrationCommand"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        public virtual async Task<bool> MigrateAsync(CancellationToken cancellationToken = default)
        {
            // Where only creating a new script, then quickly do it and get out of here!
            if (Args.MigrationCommand.HasFlag(MigrationCommand.Script))
                return await CreateScriptAsync(Args.ScriptName, Args.ScriptArguments, cancellationToken).ConfigureAwait(false);

            // Where only executing SQL statement, then execute and get out of here!
            if (Args.MigrationCommand.HasFlag(MigrationCommand.Execute))
                return await ExecuteSqlStatementsAsync(Args.ExecuteStatements?.ToArray() ?? Array.Empty<string>(), cancellationToken).ConfigureAwait(false);

            /* The remaining commands are executed in sequence as defined (where selected) to enable multiple in the correct run order. */

            // Database drop.
            if (!await CommandExecuteAsync(MigrationCommand.Drop, "DATABASE DROP: Checking database existence and dropping where found...", DatabaseDropAsync, null, cancellationToken).ConfigureAwait(false))
                return false;

            // Database create.
            if (!await CommandExecuteAsync(MigrationCommand.Create, "DATABASE CREATE: Checking database existence and creating where not found...", DatabaseCreateAsync, null, cancellationToken).ConfigureAwait(false))
                return false;

            // Database migration scripts.
            if (!await CommandExecuteAsync(MigrationCommand.Migrate, "DATABASE MIGRATE: Migrating the database...", DatabaseMigrateAsync, null, cancellationToken).ConfigureAwait(false))
                return false;

            // Code-generation (where supported).
            if (Args.MigrationCommand == MigrationCommand.CodeGen && !IsCodeGenEnabled)
            {
                Logger.LogWarning("Code-generation has not been enabled for the database migrator; this feature must be explicitly enabled.");
                return false;
            }

            if (IsCodeGenEnabled)
            {
                string? statistics = null;
                string func() => statistics ?? throw new InvalidOperationException("Internal error; expected summary text output from code-generation.");
                if (!await CommandExecuteAsync(MigrationCommand.CodeGen, "DATABASE CODEGEN: Code-gen database objects...", async ct =>
                {
                    var (Success, Statistics) = await DatabaseCodeGenAsync(ct).ConfigureAwait(false);
                    statistics = Statistics;
                    return Success;
                }, func, cancellationToken).ConfigureAwait(false))
                    return false;
            }

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
            var isSelected = Args.MigrationCommand.HasFlag(command);

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
        /// <param name="isSelected">Indicates whether the <paramref name="command"/> is selected (see <see cref="MigratorConsoleArgsBase.MigrationCommand"/>).</param>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        /// <remarks>This will be invoked for a command even where not selected for execution.</remarks>
        protected virtual Task<bool> OnBeforeCommandAsync(MigrationCommand command, bool isSelected) => Task.FromResult(true);

        /// <summary>
        /// Provides an opportunity to perform additional processing <i>after</i> the <paramref name="command"/> is executed.
        /// </summary>
        /// <param name="command">The <see cref="MigrationCommand"/>.</param>
        /// <param name="isSelected">Indicates whether the <paramref name="command"/> is selected (see <see cref="MigratorConsoleArgsBase.MigrationCommand"/>).</param>
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
        protected virtual async Task<bool> ExecuteScriptsAsync(IEnumerable<DatabaseMigrationScript> scripts, bool includeExecutionLogging, CancellationToken cancellationToken)
        {
            await Journal.EnsureExistsAsync(cancellationToken).ConfigureAwait(false);
            HashSet<string>? previous = null;
            bool somethingExecuted = false;

            foreach (var script in scripts.OrderBy(x => x.GroupOrder).ThenBy(x => x.Name))
            {
                if (!script.RunAlways)
                {
                    previous ??= new(await Journal.GetExecutedScriptsAsync(default).ConfigureAwait(false));
                    if (previous.Contains(script.Name))
                        continue;
                }

                if (includeExecutionLogging)
                    Logger.LogInformation("    {Content} {Tag}", script.Name, script.Tag ?? "");

                try
                {
                    await ExecuteScriptAsync(script, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.LogCritical(ex, "An error occured executing the script: {Message}", ex.Message);
                    return false;
                }

                await Journal.AuditScriptExecutionAsync(script, default).ConfigureAwait(false);
                somethingExecuted = true;
            }

            if (includeExecutionLogging && !somethingExecuted)
                Logger.LogInformation("    {Content}", "No new scripts found to execute.");

            return true;
        }

        /// <summary>
        /// Execute the <paramref name="script"/> (which may contain multiple commands).
        /// </summary>
        /// <param name="script">The <see cref="DatabaseMigrationScript"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        protected abstract Task ExecuteScriptAsync(DatabaseMigrationScript script, CancellationToken cancellationToken);

        /// <summary>
        /// Performs the <see cref="MigrationCommand.Drop"/> command.
        /// </summary>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>This is invoked by using the <see cref="CommandExecuteAsync(string, Func{CancellationToken, Task{bool}}, Func{string}?, CancellationToken)"/>.
        /// <para>The <c>@DatabaseName</c> literal within the resulting (embedded resource) command is replaced by the <see cref="DatabaseName"/> using a <see cref="string.Replace(string, string)"/> (i.e. not database parameterized as not all databases support).</para></remarks>
        protected virtual async Task<bool> DatabaseDropAsync(CancellationToken cancellationToken = default)
        {
            Logger.LogInformation("  Drop database...");

            using var sr = StreamLocator.GetResourcesStreamReader($"{Provider}.DatabaseDrop.sql", ArtefactResourceAssemblies).StreamReader!;
            var message = await MasterDatabase.SqlStatement(sr.ReadToEnd().Replace("@DatabaseName", DatabaseName)).ScalarAsync<string>(cancellationToken);

            Logger.LogInformation("    {Content}", message);
            return true;
        }

        /// <summary>
        /// Performs the <see cref="MigrationCommand.Create"/> command.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        /// <remarks>This is invoked by using the <see cref="CommandExecuteAsync(string, Func{CancellationToken, Task{bool}}, Func{string}?, CancellationToken)"/>.
        /// <para>The <c>@DatabaseName</c> literal within the resulting (embedded resource) is replaced by the <see cref="DatabaseName"/> using a <see cref="string.Replace(string, string)"/> (i.e. not database parameterized as not all databases support).</para></remarks>
        protected virtual async Task<bool> DatabaseCreateAsync(CancellationToken cancellationToken = default)
        {
            Logger.LogInformation("  Create database...");

            using var sr = StreamLocator.GetResourcesStreamReader($"{Provider}.DatabaseCreate.sql", ArtefactResourceAssemblies).StreamReader!;
            var message = await MasterDatabase.SqlStatement(sr.ReadToEnd().Replace("@DatabaseName", DatabaseName)).ScalarAsync<string>(cancellationToken);

            Logger.LogInformation("    {Content}", message);
            return true;
        }

        /// <summary>
        /// Performs the <see cref="MigrationCommand.Migrate"/> command.
        /// </summary>
        private async Task<bool> DatabaseMigrateAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("{Content}", $"  Probing for embedded resources: {string.Join(", ", GetNamespacesWithSuffix($"{MigrationsNamespace}.*.sql"))}");

            var scripts = new List<DatabaseMigrationScript>();
            foreach (var ass in Args.Assemblies)
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
        /// Performs the <see cref="MigrationCommand.CodeGen"/> command.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>. Additionally, on success the code-generation statistics summary should be returned to append to the log.</returns>
        /// <remarks>This will only be invoked where <see cref="IsCodeGenEnabled"/> is set to <c>true</c>. The method must be implemented otherwise a <see cref="NotImplementedException"/> will be thrown.</remarks>
        protected virtual Task<(bool Success, string? Statistics)> DatabaseCodeGenAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException($"The {nameof(DatabaseCodeGenAsync)} method must be implemented by the inheriting class to enable the code-generation functionality.");

        /// <summary>
        /// Performs the <see cref="MigrationCommand.Schema"/> command.
        /// </summary>
        private async Task<bool> DatabaseSchemaAsync(CancellationToken cancellationToken)
        {
            if (SchemaObjectTypes.Length == 0)
            {
                Logger.LogWarning("{Content}", $"  No schema object types have been configured for support; as such this command will not be executed.");
                return true;
            }

            // Build list of all known schema type objects to be dropped and created.
            var scripts = new List<DatabaseMigrationScript>();

            // See if there are any files out there that should take precedence over embedded resources.
            if (Args.OutputDirectory != null)
            {
                var di = new DirectoryInfo(Path.Combine(Args.OutputDirectory.FullName, SchemaNamespace));
                Logger.LogInformation("{Content}", $"  Probing for files (recursively): {Path.Combine(di.FullName, "*", "*.sql")}");

                if (di.Exists)
                {
                    foreach (var fi in di.GetFiles("*.sql", SearchOption.AllDirectories))
                    {
                        var rn = $"{fi.FullName[(Args.OutputDirectory.Parent.FullName.Length + 1)..]}".Replace(' ', '_').Replace('-', '_').Replace('\\', '.').Replace('/', '.');
                        scripts.Add(new DatabaseMigrationScript(fi, rn));
                    }
                }
            }

            // Get all the resources from the assemblies.
            Logger.LogInformation("{Content}", $"  Probing for embedded resources: {string.Join(", ", GetNamespacesWithSuffix($"{SchemaNamespace}.*.sql"))}");
            foreach (var ass in Args.Assemblies)
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
        /// <param name="migrationScripts">The <see cref="DatabaseMigrationScript"/> list discovered during the file and resource probes.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        /// <remarks>This is invoked by using the <see cref="CommandExecuteAsync(string, Func{CancellationToken, Task{bool}}, Func{string}?, CancellationToken)"/>.</remarks>
        protected virtual async Task<bool> DatabaseSchemaAsync(List<DatabaseMigrationScript> migrationScripts, CancellationToken cancellationToken = default)
        {
            // Parse each migration script and convert to the corresponding schema script.
            var list = new List<DatabaseSchemaScriptBase>();
            foreach (var migrationScript in migrationScripts)
            {
                var script = ValidateAndReadySchemaScript(CreateSchemaScript(migrationScript));
                if (script.HasError)
                {
                    Logger.LogError("{Message}", $"SQL script '{migrationScript.Name}' is not valid: {script.ErrorMessage}");
                    return false;
                }

                list.Add(script);
            }

            // Drop all existing (in reverse order).
            int i = 0;
            var ss = new List<DatabaseMigrationScript>();
            Logger.LogInformation("  Drop known schema objects...");
            foreach (var sor in list.OrderByDescending(x => x.SchemaOrder).ThenByDescending(x => x.TypeOrder).ThenByDescending(x => x.Schema).ThenByDescending(x => x.Name))
            {
                ss.Add(new DatabaseMigrationScript(sor.SqlDropStatement, sor.SqlDropStatement) { GroupOrder = i++, RunAlways = true });
            }

            if (!await ExecuteScriptsAsync(ss, true, cancellationToken).ConfigureAwait(false))
                return false;

            // Execute each migration script proper (i.e. create 'em as scripted).
            i = 0;
            ss.Clear();
            Logger.LogInformation("  Create known schema objects...");
            foreach (var sor in list.OrderBy(x => x.SchemaOrder).ThenBy(x => x.TypeOrder).ThenBy(x => x.Schema).ThenBy(x => x.Name))
            {
                var migrationScript = sor.MigrationScript;
                migrationScript.GroupOrder = i++;
                migrationScript.RunAlways = true;
                migrationScript.Tag = sor.SqlCreateStatement;
                ss.Add(migrationScript);
            }

            return await ExecuteScriptsAsync(ss, true, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a corresponding <see cref="DatabaseSchemaScriptBase"/> from the <paramref name="migrationScript"/>.
        /// </summary>
        /// <param name="migrationScript">The <see cref="DatabaseMigrationScript"/>.</param>
        /// <returns>The corresponding <see cref="DatabaseSchemaScriptBase"/>.</returns>
        protected abstract DatabaseSchemaScriptBase CreateSchemaScript(DatabaseMigrationScript migrationScript);

        /// <summary>
        /// Validate and ready schema (assign type and schema order) script.
        /// </summary>
        private DatabaseSchemaScriptBase ValidateAndReadySchemaScript(DatabaseSchemaScriptBase script)
        {
            if (script.HasError)
                return script;

            var index = Array.FindIndex(SchemaObjectTypes, x => string.Compare(x, script.Type, StringComparison.OrdinalIgnoreCase) == 0);
            if (index < 0)
            {
                script.ErrorMessage = $"The SQL statement `CREATE` with object type '{script.Type}' is not supported; only the following are supported: {string.Join(", ", SchemaObjectTypes)}.";
                return script;
            }

            script.TypeOrder = index;

            if (script.Schema != null)
                script.SchemaOrder = Args.SchemaOrder.IndexOf(script.Schema);

            if (script.SchemaOrder < 0)
                script.SchemaOrder = Args.SchemaOrder.Count;

            return script;
        }

        /// <summary>
        /// Performs the <see cref="MigrationCommand.Reset"/> command.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        /// <remarks>This is invoked by using the <see cref="CommandExecuteAsync(string, Func{CancellationToken, Task{bool}}, Func{string}?, CancellationToken)"/>.</remarks>
        protected virtual async Task<bool> DatabaseResetAsync(CancellationToken cancellationToken = default)
        {
            Logger.LogInformation("  Querying database to infer table(s) schema...");

            var tables = await Database.SelectSchemaAsync(null, cancellationToken).ConfigureAwait(false);
            var query = tables.Where(DataResetFilterPredicate);
            if (Args.DataResetFilterPredicate != null)
                query = query.Where(Args.DataResetFilterPredicate);

            Logger.LogInformation("  Deleting data from all tables (except filtered)...");
            var delete = query.Where(x => !x.IsAView).ToList();
            if (delete.Count == 0)
            {
                Logger.LogInformation("    None.");
                return true;
            }

            using var sr = StreamLocator.GetResourcesStreamReader($"{Provider}.DatabaseReset_sql", ArtefactResourceAssemblies, StreamLocator.HandlebarsExtensions).StreamReader!;
            var cg = new HandlebarsCodeGenerator(sr);
            var sql = cg.Generate(delete);

            using var sr2 = new StringReader(sql);
            string line;
            while ((line = sr2.ReadLine()) != null)
            {
                Logger.LogInformation("{Content}", $"    {line}");
            }

            await Database.SqlStatement(sql).SelectQueryAsync(dr => { Logger.LogInformation("{Content}", $"    {dr.GetValue<string>("FQN")}"); return 0; }, cancellationToken).ConfigureAwait(false);

            return true;
        }

        /// <summary>
        /// Gets the <see cref="MigrationCommand.Reset"/> table filtering predicate.
        /// </summary>
        /// <remarks>Used to filter out any system or internal tables that should not be reset. The <see cref="MigratorConsoleArgsBase.DataResetFilterPredicate"/> is applied after this predicate (i.e. it can not override the base filtering).</remarks>
        protected abstract Func<DbSchema.DbTableSchema, bool> DataResetFilterPredicate { get; }

        /// <summary>
        /// Performs the <see cref="MigrationCommand.Data"/> command.
        /// </summary>
        private async Task<bool> DatabaseDataAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("{Content}", $"  Probing for embedded resources: {string.Join(", ", GetNamespacesWithSuffix($"{DataNamespace}.*.[sql|yaml]", true))}");

            var list = new List<(Assembly Assembly, string ResourceName)>();
            foreach (var ass in Args.Assemblies)
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
            var dbTables = await Database.SelectSchemaAsync(Args.DataParserArgs.RefDataPredicate, cancellationToken).ConfigureAwait(false);

            // Iterate through each resource - parse the data, then insert/merge as requested.
            var parser = new DataParser(dbTables, Args.DataParserArgs);
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
        protected virtual async Task<bool> DatabaseDataAsync(List<DataTable> dataTables, CancellationToken cancellationToken = default)
        {
            // Cache the compiled code-gen template.
            if (_dataCodeGen == null)
            {
                using var sr = StreamLocator.GetResourcesStreamReader($"{Provider}.DatabaseData_sql", ArtefactResourceAssemblies, StreamLocator.HandlebarsExtensions).StreamReader!;
                _dataCodeGen = new HandlebarsCodeGenerator(await sr.ReadToEndAsync().ConfigureAwait(false));
            }

            foreach (var table in dataTables)
            {
                Logger.LogInformation("");
                Logger.LogInformation("{Content}", $"---- Executing {table.Schema}.{table.Name} SQL:");

                var sql = _dataCodeGen.Generate(table);
                Logger.LogInformation("{Content}", sql);

                var rows = await Database.SqlStatement(sql).ScalarAsync<int>(cancellationToken).ConfigureAwait(false);
                Logger.LogInformation("{Content}", $"Result: {rows} rows affected.");
            }

            return true;
        }

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
        /// Creates a new script using the <paramref name="name"/> template within the <see cref="MigrationsNamespace"/> folder.
        /// </summary>
        /// <param name="name">The script resource template name; defaults to '<c>default</c>'.</param>
        /// <param name="parameters">The optional parameters.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        public async Task<bool> CreateScriptAsync(string? name = null, IDictionary<string, string?>? parameters = null, CancellationToken cancellationToken = default)
            => await CommandExecuteAsync("DATABASE SCRIPT: Create a new database script...", async ct => await CreateScriptInternalAsync(name, parameters, ct).ConfigureAwait(false), null, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Creates the new script.
        /// </summary>
        private async Task<bool> CreateScriptInternalAsync(string? name, IDictionary<string, string?>? parameters, CancellationToken cancellationToken)
        {
            name ??= "Default";
            var rn = $"{Provider}.Script{name}_sql";

            // Find the resource.
            using var sr = StreamLocator.GetResourcesStreamReader(rn, ArtefactResourceAssemblies, StreamLocator.HandlebarsExtensions).StreamReader;

            if (sr == null)
            {
                Logger.LogError("{Content}", $"The Script '{name}' does not exist.");
                return false;
            }

            // Read the resource stream.
            var txt = await sr.ReadToEndAsync().ConfigureAwait(false);

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
            if (Args.OutputDirectory == null)
                throw new InvalidOperationException("Args.OutputDirectory has not been correctly determined.");

            fn = $"{DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture)}-{fn.Replace("[", "{{").Replace("]", "}}")}.sql";
            fn = Path.Combine(Args.OutputDirectory.FullName, MigrationsNamespace, new HandlebarsCodeGenerator(fn).Generate(data).Replace(" ", "-").ToLowerInvariant());
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