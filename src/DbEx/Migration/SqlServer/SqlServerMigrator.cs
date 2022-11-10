// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.Database;
using CoreEx.Database.SqlServer;
using DbEx.Console;
using DbEx.DbSchema;
using DbUp.Support;
using Microsoft.Data.SqlClient;
using OnRamp.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DbEx.Migration.SqlServer
{
    /// <summary>
    /// Provides the <see href="https://docs.microsoft.com/en-us/sql/connect/ado-net/microsoft-ado-net-sql-server">SQL Server</see> migration orchestration.
    /// </summary>
    public class SqlServerMigrator : DatabaseMigratorBase
    {
        private readonly string _databaseName;
        private readonly IDatabase _database;
        private readonly IDatabase _masterDatabase;

        /// <summary>
        /// Initializes an instance of the <see cref="SqlServerMigrator"/> class.
        /// </summary>
        /// <param name="args">The <see cref="MigratorConsoleArgs"/>.</param>
        public SqlServerMigrator(MigratorConsoleArgs args) : base(args)
        {
            var csb = new SqlConnectionStringBuilder(Args.ConnectionString);
            _databaseName = csb.InitialCatalog;
            if (string.IsNullOrEmpty(_databaseName))
                throw new ArgumentException($"The {nameof(OnRamp.CodeGeneratorDbArgsBase.ConnectionString)} property must contain an initial catalog (i.e. database name).", nameof(args));

            _database = new SqlServerDatabase(() => new SqlConnection(Args.ConnectionString));

            csb.InitialCatalog = "master";
            _masterDatabase = new SqlServerDatabase(() => new SqlConnection(csb.ConnectionString));

            // Where no data reset predicate filter added then default to exclude 'dbo' and 'cdc'; where a dev needs to do all then they can override with following predicate: schema => true;
            if (Args.DataResetFilterPredicate == null)
                Args.DataResetFilterPredicate = schema => schema.Schema != "dbo" || schema.Schema != "cdc";
        }

        /// <inheritdoc/>
        public override string Provider => "SqlServer";

        /// <inheritdoc/>
        public override string DatabaseName => _databaseName;

        /// <inheritdoc/>
        public override IDatabase Database => _database;

        /// <inheritdoc/>
        protected override IDatabase MasterDatabase => _masterDatabase;

        /// <inheritdoc/>
        protected override string[] KnownSchemaObjectTypes => new string[] { "TYPE", "FUNCTION", "VIEW", "PROCEDURE", "PROC" };

        /// <inheritdoc/>
        protected override DatabaseSchemaScriptBase CreateSchemaScript(DatabaseMigrationScript migrationScript) => SqlServerSchemaScript.Create(migrationScript);

        /// <inheritdoc/>
        protected override Func<DbTableSchema, bool> DataResetFilterPredicate => schema => !(schema.Schema == "dbo" && schema.Name == "SchemaVersions");

        /// <inheritdoc/>
        protected override async Task ExecuteScriptAsync(DatabaseMigrationScript script, CancellationToken cancellationToken = default)
        {
            using var sr = script.GetStreamReader();

            foreach (var sql in new SqlCommandSplitter().SplitScriptIntoCommands(sr.ReadToEnd()))
            {
                await Database.SqlStatement(sql).NonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}