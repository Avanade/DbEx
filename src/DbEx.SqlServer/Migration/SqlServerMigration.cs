﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.Database;
using CoreEx.Database.SqlServer;
using DbEx.DbSchema;
using DbEx.Migration;
using DbUp.Support;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using OnRamp.Utility;
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
        private List<string> _resetBypass = new List<string>();

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

            // Where no data reset predicate filter added then default to exclude 'dbo' and 'cdc'; where a dev needs to do all then they can override with following predicate: schema => true;
            if (Args.DataResetFilterPredicate == null)
                Args.DataResetFilterPredicate = schema => schema.Schema != "dbo" || schema.Schema != "cdc";

            // Defaults the schema object types unless already specified.
            if (SchemaObjectTypes.Length == 0)
                SchemaObjectTypes = new string[] { "TYPE", "FUNCTION", "VIEW", "PROCEDURE", "PROC" };

            // Always add the dbo schema _first_ unless already specified.
            if (!Args.SchemaOrder.Contains("dbo"))
                Args.SchemaOrder.Insert(0, "dbo");

            Journal.Schema = "dbo";
            Journal.Table = "SchemaVersions";
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
        protected override DatabaseSchemaScriptBase CreateSchemaScript(DatabaseMigrationScript migrationScript) => SqlServerSchemaScript.Create(migrationScript);

        /// <inheritdoc/>
        protected override async Task<bool> DatabaseResetAsync(CancellationToken cancellationToken = default)
        {
            // Filter out temporal tables.
            Logger.LogInformation("  Querying database to find and filter all temporal table(s)...");
            using var sr = StreamLocator.GetResourcesStreamReader($"DatabaseTemporal.sql", ArtefactResourceAssemblies.ToArray()).StreamReader!;
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
        protected override Func<DbTableSchema, bool> DataResetFilterPredicate => schema => !_resetBypass.Contains(schema.QualifiedName!);

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