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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
        protected async override Task<bool> DatabaseDropAsync(CancellationToken cancellationToken = default)
        {
            Logger.LogInformation("  Drop database...");

            var mdb = GetMasterDatabase();
            if (mdb == null)
                return false;

            using var sr = StreamLocator.GetResourcesStreamReader("SqlServer.DatabaseDrop.sql", new Assembly[] { typeof(SqlServerJournal).Assembly }).StreamReader!;
            var message = await mdb.SqlStatement(sr.ReadToEnd().Replace("@DatabaseName", new SqlConnectionStringBuilder(ConnectionString).InitialCatalog)).ScalarAsync<string>(cancellationToken);

            Logger.LogInformation("    {Content}", message);
            return true;
        }

        /// <inheritdoc/>
        protected async override Task<bool> DatabaseCreateAsync(CancellationToken cancellationToken = default)
        {
            Logger.LogInformation("  Create database...");

            var mdb = GetMasterDatabase();
            if (mdb == null)
                return false;

            using var sr = StreamLocator.GetResourcesStreamReader("SqlServer.DatabaseCreate.sql", new Assembly[] { typeof(SqlServerJournal).Assembly }).StreamReader!;
            var message = await mdb.SqlStatement(sr.ReadToEnd().Replace("@DatabaseName", new SqlConnectionStringBuilder(ConnectionString).InitialCatalog)).ScalarAsync<string>(cancellationToken);

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
        protected override async Task<bool> DatabaseSchemaAsync(List<DatabaseMigrationScript> scripts, CancellationToken cancellationToken = default)
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

            if (!await ExecuteScriptsAsync(ss, true, cancellationToken).ConfigureAwait(false))
                return false;

            // Execute each script proper.
            i = 0;
            ss.Clear();
            Logger.LogInformation("  Create known schema objects...");
            foreach (var sor in list.OrderBy(x => x.SchemaOrder).ThenBy(x => x.TypeOrder).ThenBy(x => x.Name))
            {
                ss.Add(new DatabaseMigrationScript(sor.GetSql(), sor.ScriptName) { GroupOrder = i++, RunAlways = true, Tag = $">> CREATE {sor.Type} [{sor.Schema}].[{sor.Name}]" });
            }

            return await ExecuteScriptsAsync(ss, true, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override async Task<bool> DatabaseResetAsync(CancellationToken cancellationToken = default)
        {
            Logger.LogInformation("  Deleting data from all tables (excludes schema 'dbo' and 'cdc')...");
            using var sr = StreamLocator.GetResourcesStreamReader("SqlServer.DeleteAllAndReset.sql", new Assembly[] { typeof(DatabaseExtensions).Assembly }).StreamReader!;
            var tables = await Database.SqlStatement(sr.ReadToEnd()).SelectQueryAsync(dr => { Logger.LogInformation("{Content}", $"    [{dr.GetValue<string>("Schema")}].[{dr.GetValue<string>("Table")}]"); return 0; }, cancellationToken).ConfigureAwait(false);
            if (!tables.Any())
                Logger.LogInformation("    None.");

            return true;
        }

        /// <inheritdoc/>
        protected override async Task<bool> DatabaseDataAsync(List<DataTable> dataTables, CancellationToken cancellationToken = default)
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

                var rows = await Database.SqlStatement(sql).ScalarAsync<int>(cancellationToken).ConfigureAwait(false);
                Logger.LogInformation("{Content}", $"Result: {rows} rows affected.");
            }

            return true;
        }

        /// <inheritdoc/>
        protected override IDatabase CreateDatabase(string connectionString) => new SqlServerDatabase(() => new SqlConnection(connectionString));

        /// <inheritdoc/>
        public override async Task<bool> ExecuteScriptsAsync(IEnumerable<DatabaseMigrationScript> scripts, bool includeExecutionLogging, CancellationToken cancellationToken = default)
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
        /// Execute the script as potentially multiple within a batch context; i.e. there is a GO statement.
        /// </summary>
        private async Task ExecuteScriptAsync(DatabaseMigrationScript script, CancellationToken cancellationToken = default)
        {
            using var sr = script.GetStreamReader();
            foreach (var sql in SplitSql(CleanSql(sr)))
            {
                await Database.SqlStatement(sql).NonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// <see cref="CleanSql(TextReader)">Cleans</see> and splits (based on the SQL Server '<c><see href="http://technet.microsoft.com/en-us/library/ms188037.aspx">GO</see></c>' statement) the SQL command(s) from a <see cref="TextReader"/>.
        /// </summary>
        /// <param name="tr">The <see cref="TextReader"/>.</param>
        /// <returns>A list of executable SQL statements.</returns>
        /// <remarks>The '<c>GO [count]</c>' count syntax is not supported; i.e. will not be parsed correctly and will likely result in an error when executed.</remarks>
        public static List<string> CleanAndSplitSql(TextReader tr) => SplitSql(CleanSql(tr));

        /// <summary>
        /// Split the SQL into multiple statements based on the SQL Server '<c><see href="http://technet.microsoft.com/en-us/library/ms188037.aspx">GO</see></c>' statement.
        /// </summary>
        /// <param name="sql">The originating SQL.</param>
        /// <returns>A list of executable SQL statements.</returns>
        private static List<string> SplitSql(string sql)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(sql))
                return list;

            var sb = new StringBuilder();
            string? line;
            using var tr = new StringReader(sql);

            while ((line = tr.ReadLine()) is not null)
            {
                if (line.Trim().Equals("GO", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (sb.Length > 0 && !string.IsNullOrEmpty(sb.ToString().Trim()))
                        list.Add(sb.ToString());

                    sb.Clear();
                }
                else
                {
                    if (sb.Length > 0)
                        sb.AppendLine();

                    sb.Append(line);
                }
            }

            if (sb.Length > 0 && !string.IsNullOrEmpty(sb.ToString().Trim()))
                list.Add(sb.ToString());

            return list;
        }

        /// <summary>
        /// Reads the SQL command from a <see cref="TextReader"/> and parses out all of the comments.
        /// </summary>
        /// <param name="tr">The <see cref="TextReader"/>.</param>
        public static string CleanSql(TextReader tr)
        {
            string? line;
            bool inComment = false;
            StringBuilder sb = new();

            while ((line = tr.ReadLine()) is not null)
            {
                int ci;

                // Remove /* */ comments
                if (inComment)
                {
                    ci = line.IndexOf("*/");
                    if (ci < 0)
                        continue;

                    line = line[(ci + 2)..];
                    inComment = false;

                    if (string.IsNullOrEmpty(line))
                        continue;
                }

                ci = line.IndexOf("/*");
                if (ci >= 0)
                {
                    var ci2 = line.IndexOf("*/");
                    if (ci2 >= 0)
                        line = line[0..ci] + line[(ci2 + 2)..];
                    else
                    {
                        inComment = true;
                        continue;
                    }
                }

                // Remove -- comments.
                ci = line.IndexOf("--", StringComparison.InvariantCulture);
                if (ci >= 0)
                    line = line[..ci];

                // Add as valid statement.
                if (sb.Length > 0)
                    sb.AppendLine();

                sb.Append(line);
            }

            return sb.ToString();
        }
    }
}