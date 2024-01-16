// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.Database;
using CoreEx.Database.SqlServer;
using DbEx.DbSchema;
using DbEx.Migration;
using DbUp.Support;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DbEx.SqlServer.Migration
{
    /// <summary>
    /// Provides the <see href="https://docs.microsoft.com/en-us/sql/connect/ado-net/microsoft-ado-net-sql-server">SQL Server</see> migration orchestration.
    /// </summary>
    /// <remarks>The following <see cref="DatabaseMigrationBase.SchemaObjectTypes"/> are supported by default: '<c>TYPE</c>', '<c>FUNCTION</c>', '<c>VIEW</c>', '<c>PROCEDURE</c>' and '<c>PROC</c>'.
    /// <para>Where the <see cref="DatabaseMigrationBase.Args"/> <see cref="MigrationArgsBase.DataResetFilterPredicate"/> is not specified it will default to '<c>schema => schema.Schema != "dbo" || schema.Schema != "cdc"</c>' which will 
    /// filter out a data reset where a table is in the '<c>dbo</c>' and '<c>cdc</c>' schemas.</para>
    /// <para>The base <see cref="DatabaseMigrationBase.Journal"/> instance is updated; the <see cref="IDatabaseJournal.Schema"/> and <see cref="IDatabaseJournal.Table"/> properties are set to `<c>dbo</c>` and `<c>SchemaVersions</c>` respectively.</para></remarks>
    public class SqlServerMigration : DatabaseMigrationBase
    {
        private readonly string _databaseName;
        private readonly IDatabase _database;
        private readonly IDatabase _masterDatabase;
        private readonly List<string> _resetBypass = [];

        /// <summary>
        /// Initializes an instance of the <see cref="SqlServerMigration"/> class.
        /// </summary>
        /// <param name="args">The <see cref="MigrationArgsBase"/>.</param>
        public SqlServerMigration(MigrationArgsBase args) : base(args)
        {
            var csb = new SqlConnectionStringBuilder(Args.ConnectionString);
            _databaseName = csb.InitialCatalog;
            if (string.IsNullOrEmpty(_databaseName))
                throw new ArgumentException($"The {nameof(OnRamp.CodeGeneratorDbArgsBase.ConnectionString)} property must contain an initial catalog (i.e. database name).", nameof(args));

            _database = new SqlServerDatabase(() => new SqlConnection(Args.ConnectionString));

            csb.InitialCatalog = "master";
            _masterDatabase = new SqlServerDatabase(() => new SqlConnection(csb.ConnectionString));

            // Add this assembly for probing.
            Args.AddAssemblyAfter(typeof(DatabaseMigrationBase).Assembly, typeof(SqlServerMigration).Assembly);

            // Defaults the schema object types unless already specified.
            if (SchemaObjectTypes.Length == 0)
                SchemaObjectTypes = ["TYPE", "FUNCTION", "VIEW", "PROCEDURE", "PROC"];

            // Always add the dbo schema _first_ unless already specified.
            if (!Args.SchemaOrder.Contains("dbo"))
                Args.SchemaOrder.Insert(0, "dbo");

            // Add/set standard parameters.
            Args.Parameter(MigrationArgsBase.DatabaseNameParamName, _databaseName, true);
            Args.Parameter(MigrationArgsBase.JournalSchemaParamName, "dbo");
            Args.Parameter(MigrationArgsBase.JournalTableParamName, "SchemaVersions");
        }

        /// <inheritdoc/>
        public override string Provider => "SqlServer";

        /// <inheritdoc/>
        public override string DatabaseName => _databaseName;

        /// <inheritdoc/>
        public override IDatabase Database => _database;

        /// <inheritdoc/>
        public override IDatabase MasterDatabase => _masterDatabase;

        /// <inheritdoc/>
        public override DatabaseSchemaConfig DatabaseSchemaConfig => new SqlServerSchemaConfig(DatabaseName);

        /// <inheritdoc/>
        protected override DatabaseSchemaScriptBase CreateSchemaScript(DatabaseMigrationScript migrationScript) => SqlServerSchemaScript.Create(migrationScript);

        /// <inheritdoc/>
        protected override async Task<bool> DatabaseResetAsync(CancellationToken cancellationToken = default)
        {
            // Filter out temporal tables.
            Logger.LogInformation("  Querying database to find and filter all temporal table(s)...");
            using var sr = GetRequiredResourcesStreamReader($"DatabaseTemporal.sql", ArtefactResourceAssemblies.ToArray());
            await Database.SqlStatement(sr.ReadToEnd()).SelectQueryAsync(dr =>
            {
                _resetBypass.Add($"[{dr.GetValue<string>("schema")}].[{dr.GetValue<string>("table")}]");
                return 0;
            }, cancellationToken).ConfigureAwait(false);

            // Filter out the versioning table.
            _resetBypass.Add($"[{Journal.Schema}].[{Journal.Table}]");

            // Carry on as they say ;-)
            return await base.DatabaseResetAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override Func<DbTableSchema, bool> DataResetFilterPredicate => 
            schema => !_resetBypass.Contains(schema.QualifiedName!) && schema.Schema != "sys" && schema.Schema != "cdc" && !(schema.Schema == "dbo" && schema.Name.StartsWith("sys"));

        /// <inheritdoc/>
        protected override async Task ExecuteScriptAsync(DatabaseMigrationScript script, CancellationToken cancellationToken = default)
        {
            using var sr = script.GetStreamReader();

            foreach (var sql in new SqlCommandSplitter().SplitScriptIntoCommands(sr.ReadToEnd()))
            {
                await Database.SqlStatement(ReplaceSqlRuntimeParameters(sql)).NonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}