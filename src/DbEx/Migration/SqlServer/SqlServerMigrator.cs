﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.Migration.Data;
using DbEx.Migration.SqlServer.Internal;
using DbUp;
using DbUp.Engine;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using OnRamp.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DbEx.Migration.SqlServer
{
    /// <summary>
    /// Provides the <see href="https://docs.microsoft.com/en-us/sql/connect/ado-net/microsoft-ado-net-sql-server">SQL Server</see> migration orchestration leveraging <see href="https://dbup.readthedocs.io/en/latest/">DbUp</see>.
    /// </summary>
    public class SqlServerMigrator : DatabaseMigratorBase
    {
        private HandlebarsCodeGenerator? _codeGen;

        /// <summary>
        /// Initializes an instance of the <see cref="SqlServerMigrator"/> class.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="command">The <see cref="MigrationCommand"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="assemblies">The <see cref="Assembly"/> list to use to probe for assembly resource (in specified sequence).</param>
        public SqlServerMigrator(string connectionString, MigrationCommand command, ILogger logger, params Assembly[] assemblies)
            : base(connectionString, command, logger, assemblies) { }

        /// <inheritdoc/>
        protected override string[] KnownSchemaObjectTypes => new string[] { "TYPE", "FUNCTION", "VIEW", "PROCEDURE", "PROC" };

        /// <inheritdoc/>
        protected override Task<bool> DatabaseDropAsync()
        {
            Logger.LogInformation("  Drop database (using DbUp)...");
            DropDatabase.For.SqlDatabase(ConnectionString, LoggerSink);
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        protected override Task<bool> DatabaseCreateAsync()
        {
            Logger.LogInformation("  Create database (using DbUp)...");
            EnsureDatabase.For.SqlDatabase(ConnectionString, LoggerSink);
            return Task.FromResult(true);
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
                    Logger.LogError($"SQL script '{script.Name}' is not valid: {sor.ErrorMessage}");
                    return false;
                }

                list.Add(sor);
            }

            // Drop all existing (in reverse order).
            int i = 0;
            var ss = new List<SqlScript>();
            Logger.LogInformation("  Drop (using DbUp) known schema objects...");
            foreach (var sor in list.OrderByDescending(x => x.SchemaOrder).ThenByDescending(x => x.TypeOrder).ThenByDescending(x => x.Name))
            {
                var sql = $"DROP {sor.Type} IF EXISTS [{sor.Schema}].[{sor.Name}]";
                ss.Add(new SqlScript(sql, sql, new SqlScriptOptions { RunGroupOrder = i++, ScriptType = DbUp.Support.ScriptType.RunAlways }));
            }

            var r = await DeployChangesAsync(ss).ConfigureAwait(false);
            if (!r.Successful)
                return false;

            // Execute each script proper.
            i = 0;
            ss.Clear();
            Logger.LogInformation("  Create (using DbUp) known schema objects...");
            foreach (var sor in list.OrderBy(x => x.SchemaOrder).ThenBy(x => x.TypeOrder).ThenBy(x => x.Name))
            {
                ss.Add(new SqlScript(sor.ScriptName, sor.GetSql(), new SqlScriptOptions { RunGroupOrder = i++, ScriptType = DbUp.Support.ScriptType.RunAlways }));
            }

            return (await DeployChangesAsync(ss).ConfigureAwait(false)).Successful;
        }

        /// <inheritdoc/>
        protected override async Task<bool> DatabaseResetAsync()
        {
            using var sr = StreamLocator.GetResourcesStreamReader("SqlServer.DeleteAllAndReset.sql", typeof(IDatabase).Assembly)!;
            var ss = new SqlScript($"{typeof(IDatabase).Namespace}.SqlServer.DeleteAllAndReset.sql", await sr.ReadToEndAsync().ConfigureAwait(false), new SqlScriptOptions { ScriptType = DbUp.Support.ScriptType.RunAlways });
            return (await DeployChangesAsync(new SqlScript[] { ss }).ConfigureAwait(false)).Successful;
        }

        /// <inheritdoc/>
        protected override async Task<bool> DatabaseDataAsync(IDatabase database, List<DataTable> dataTables)
        {
            // Cache the compiled code-gen template.
            if (_codeGen == null)
            {
                using var sr = StreamLocator.GetResourcesStreamReader("SqlServer.TableInsertOrMerge_sql.hb", typeof(IDatabase).Assembly)!;
                _codeGen = new HandlebarsCodeGenerator(await sr.ReadToEndAsync().ConfigureAwait(false));
            }

            foreach (var table in dataTables)
            {
                Logger.LogInformation(string.Empty);
                Logger.LogInformation($"---- Executing {table.Schema}.{table.Name} SQL:");

                var sql = _codeGen.Generate(table);
                Logger.LogInformation(sql);

                var rows = await database.SqlStatement(sql).ScalarAsync<int>().ConfigureAwait(false);
                Logger.LogInformation($"Result: {rows} rows affected.");
            }

            Logger.LogInformation(string.Empty);
            return true;
        }

        /// <inheritdoc/>
        protected override IDatabase CreateDatabase(string connectionString) => new Database<SqlConnection>(() => new SqlConnection(connectionString));

        /// <inheritdoc/>
        protected override Task<DatabaseUpgradeResult> DeployChangesAsync(IEnumerable<SqlScript> scripts)
            => Task.FromResult(DbUp.DeployChanges.To
                .SqlDatabase(ConnectionString)
                .WithScripts(scripts)
                .WithoutTransaction()                                                                                                                                       
                .LogTo(LoggerSink)
                .Build()
                .PerformUpgrade());
    }
}