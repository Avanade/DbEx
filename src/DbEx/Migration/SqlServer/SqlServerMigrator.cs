// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.Database;
using CoreEx.Database.SqlServer;
using DbEx.Migration.Data;
using DbEx.Migration.SqlServer.Internal;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using OnRamp.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DbEx.Migration.SqlServer
{
    /// <summary>
    /// Provides the <see href="https://docs.microsoft.com/en-us/sql/connect/ado-net/microsoft-ado-net-sql-server">SQL Server</see> migration orchestration.
    /// </summary>
    public class SqlServerMigrator : DatabaseMigratorBase
    {
        private IDatabase? _database;
        private IDatabaseJournal? _journal;
        private HandlebarsCodeGenerator? _codeGen;

        /// <summary>
        /// Initializes an instance of the <see cref="SqlServerMigrator"/> class.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="command">The <see cref="MigrationCommand"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="assemblies">The <see cref="Assembly"/> list to use to probe for assembly resource (in specified sequence).</param>
        public SqlServerMigrator(string connectionString, MigrationCommand command, ILogger logger, params Assembly[] assemblies) : base(connectionString, command, logger, assemblies) { }

        /// <inheritdoc/>
        protected override IDatabase Database => _database ??= new SqlServerDatabase(() => new SqlConnection(ConnectionString));

        /// <inheritdoc/>
        protected override IDatabaseJournal Journal => _journal ??= new SqlServerJournal(Database, Logger);

        /// <inheritdoc/>
        protected override string[] KnownSchemaObjectTypes => new string[] { "TYPE", "FUNCTION", "VIEW", "PROCEDURE", "PROC" };

        /// <summary>
        /// Gets or sets the maximum retries for <see cref="ExecuteScriptsAsync"/> in case of transient database errors on database initialization.
        /// </summary>
        public int MaxRetries { get; set; } = 5;

        /// <inheritdoc/>
        protected async override Task<bool> DatabaseDropAsync()
        {
            Logger.LogInformation("  Drop database...");

            var mdb = GetMasterDatabase();
            if (mdb == null)
                return false;

            using var sr = StreamLocator.GetResourcesStreamReader("SqlServer.DatabaseDrop.sql", new Assembly[] { typeof(SqlServerJournal).Assembly }).StreamReader!;
            var message = await mdb.SqlStatement(sr.ReadToEnd().Replace("@DatabaseName", new SqlConnectionStringBuilder(ConnectionString).InitialCatalog)).ScalarAsync<string>(default);

            Logger.LogInformation("    {Content}", message);
            return true;
        }

        /// <inheritdoc/>
        protected async override Task<bool> DatabaseCreateAsync()
        {
            Logger.LogInformation("  Create database...");

            var mdb = GetMasterDatabase();
            if (mdb == null)
                return false;

            using var sr = StreamLocator.GetResourcesStreamReader("SqlServer.DatabaseCreate.sql", new Assembly[] { typeof(SqlServerJournal).Assembly }).StreamReader!;
            var message = await mdb.SqlStatement(sr.ReadToEnd().Replace("@DatabaseName", new SqlConnectionStringBuilder(ConnectionString).InitialCatalog)).ScalarAsync<string>(default);

            Logger.LogInformation("    {Content}", message);
            return true;
        }

        /// <summary>
        /// Gets the corresponding 'master' database.
        /// </summary>
        private SqlServerDatabase? GetMasterDatabase()
        {
            var csb = new SqlConnectionStringBuilder(ConnectionString);
            if (string.IsNullOrEmpty(csb.InitialCatalog?.Trim()))
            {
                Logger.LogError("    {Message}", "The connection string does not specify a database name.");
                return null;
            }

            csb.InitialCatalog = "master";
            return new SqlServerDatabase(() => new SqlConnection(csb.ConnectionString));
        }

        /// <inheritdoc/>
        protected override async Task<bool> DatabaseSchemaAsync(List<DatabaseMigrationScript> scripts)
        {
            // Parse each script and determine type and object.
            var list = new List<SqlServerObjectReader>();
            foreach (var script in scripts)
            {
                // Read the script and validate.
                using var sr = script.GetStreamReader();
                var sor = SqlServerObjectReader.Read(script.Name, sr, KnownSchemaObjectTypes, SchemaOrder.ToArray());
                if (!sor.IsValid)
                {
                    Logger.LogError("{Message}", $"SQL script '{script.Name}' is not valid: {sor.ErrorMessage}");
                    return false;
                }

                list.Add(sor);
            }

            // Drop all existing (in reverse order).
            int i = 0;
            var ss = new List<DatabaseMigrationScript>();
            Logger.LogInformation("  Drop known schema objects...");
            foreach (var sor in list.OrderByDescending(x => x.SchemaOrder).ThenByDescending(x => x.TypeOrder).ThenByDescending(x => x.Name))
            {
                var sql = $"DROP {sor.Type} IF EXISTS [{sor.Schema}].[{sor.Name}]";
                ss.Add(new DatabaseMigrationScript(sql, sql) { GroupOrder = i++, RunAlways = true });
            }

            if (!await ExecuteScriptsAsync(ss, true).ConfigureAwait(false))
                return false;

            // Execute each script proper.
            i = 0;
            ss.Clear();
            Logger.LogInformation("  Create known schema objects...");
            foreach (var sor in list.OrderBy(x => x.SchemaOrder).ThenBy(x => x.TypeOrder).ThenBy(x => x.Name))
            {
                ss.Add(new DatabaseMigrationScript(sor.GetSql(), sor.ScriptName) { GroupOrder = i++, RunAlways = true, Tag = $">> CREATE {sor.Type} [{sor.Schema}].[{sor.Name}]" });
            }

            return await ExecuteScriptsAsync(ss, true).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override async Task<bool> DatabaseResetAsync()
        {
            Logger.LogInformation("  Deleting data from all tables (excludes schema 'dbo' and 'cdc').");
            var ss = new DatabaseMigrationScript(typeof(DatabaseExtensions).Assembly, $"DbEx.Resources.SqlServer.DeleteAllAndReset.sql") { RunAlways = true };
            return await ExecuteScriptsAsync(new DatabaseMigrationScript[] { ss }, false).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override async Task<bool> DatabaseDataAsync(List<DataTable> dataTables)
        {
            // Cache the compiled code-gen template.
            if (_codeGen == null)
            {
                using var sr = StreamLocator.GetResourcesStreamReader("SqlServer.TableInsertOrMerge_sql.hb", new Assembly[] { typeof(DatabaseExtensions).Assembly }).StreamReader!;
                _codeGen = new HandlebarsCodeGenerator(await sr.ReadToEndAsync().ConfigureAwait(false));
            }

            foreach (var table in dataTables)
            {
                Logger.LogInformation("");
                Logger.LogInformation("{Content}", $"---- Executing {table.Schema}.{table.Name} SQL:");

                var sql = _codeGen.Generate(table);
                Logger.LogInformation("{Content}", sql);

                var rows = await Database.SqlStatement(sql).ScalarAsync<int>().ConfigureAwait(false);
                Logger.LogInformation("{Content}", $"Result: {rows} rows affected.");
            }

            return true;
        }

        /// <inheritdoc/>
        protected override IDatabase CreateDatabase(string connectionString) => new SqlServerDatabase(() => new SqlConnection(connectionString));

        /// <inheritdoc/>
        public override async Task<bool> ExecuteScriptsAsync(IEnumerable<DatabaseMigrationScript> scripts, bool includeExecutionLogging)
        {
            await Journal.EnsureExistsAsync().ConfigureAwait(false);
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
                    await Database.SqlStatement(script.GetStreamReader().ReadToEnd()).NonQueryAsync(default).ConfigureAwait(false);
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
    }
}