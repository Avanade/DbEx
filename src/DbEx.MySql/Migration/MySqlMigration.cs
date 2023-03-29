// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.Database;
using CoreEx.Database.MySql;
using DbEx.DbSchema;
using DbEx.Migration;
using DbUp.Support;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbEx.MySql.Migration
{
    /// <summary>
    /// Provides the <see href="https://dev.mysql.com/">MySQL</see> migration orchestration.
    /// </summary>
    /// <remarks>The following <see cref="DatabaseMigrationBase.SchemaObjectTypes"/> are supported by default: '<c>TYPE</c>', '<c>FUNCTION</c>', '<c>VIEW</c>', '<c>PROCEDURE</c>' and '<c>PROC</c>'.
    /// <para>Where the <see cref="DatabaseMigrationBase.Args"/> <see cref="MigrationArgsBase.DataResetFilterPredicate"/> is not specified it will default to '<c>schema => schema.Schema != "dbo" || schema.Schema != "cdc"</c>' which will 
    /// filter out a data reset where a table is in the '<c>dbo</c>' and '<c>cdc</c>' schemas.</para>
    /// <para>The base <see cref="DatabaseMigrationBase.Journal"/> instance is updated; the <see cref="IDatabaseJournal.Schema"/> and <see cref="IDatabaseJournal.Table"/> properties are set to `<c>dbo</c>` and `<c>SchemaVersions</c>` respectively.</para></remarks>
    public class MySqlMigration : DatabaseMigrationBase
    {
        private readonly string _databaseName;
        private readonly IDatabase _database;
        private readonly IDatabase _masterDatabase;
        private readonly List<string> _resetBypass = new();

        /// <summary>
        /// Initializes an instance of the <see cref="MySqlMigration"/> class.
        /// </summary>
        /// <param name="args">The <see cref="MigrationArgsBase"/>.</param>
        public MySqlMigration(MigrationArgsBase args) : base(args)
        {
            var csb = new MySqlConnectionStringBuilder(Args.ConnectionString);
            _databaseName = csb.Database;
            if (string.IsNullOrEmpty(_databaseName))
                throw new ArgumentException($"The {nameof(OnRamp.CodeGeneratorDbArgsBase.ConnectionString)} property must contain a database name.", nameof(args));

            _database = new MySqlDatabase(() => new MySqlConnection(Args.ConnectionString));

            csb.Database = null;
            _masterDatabase = new MySqlDatabase(() => new MySqlConnection(csb.ConnectionString));

            // Add this assembly for probing.
            Args.AddAssemblyAfter(typeof(DatabaseMigrationBase).Assembly, typeof(MySqlMigration).Assembly);

            // Defaults the schema object types unless already specified.
            if (SchemaObjectTypes.Length == 0)
                SchemaObjectTypes = new string[] { "FUNCTION", "VIEW", "PROCEDURE" };

            // Add/set standard parameters.
            Args.Parameter(MigrationArgsBase.DatabaseNameParamName, _databaseName, true);
            Args.Parameter(MigrationArgsBase.JournalSchemaParamName, null, true);
            Args.Parameter(MigrationArgsBase.JournalTableParamName, "schemaversions");
        }

        /// <inheritdoc/>
        public override string Provider => "MySQL";

        /// <inheritdoc/>
        public override string DatabaseName => _databaseName;

        /// <inheritdoc/>
        public override IDatabase Database => _database;

        /// <inheritdoc/>
        public override IDatabase MasterDatabase => _masterDatabase;

        /// <inheritdoc/>
        public override DatabaseSchemaConfig DatabaseSchemaConfig => new MySqlSchemaConfig(DatabaseName);

        /// <inheritdoc/>
        protected override DatabaseSchemaScriptBase CreateSchemaScript(DatabaseMigrationScript migrationScript) => MySqlSchemaScript.Create(migrationScript);

        /// <inheritdoc/>
        protected override async Task<bool> DatabaseResetAsync(CancellationToken cancellationToken = default)
        {
            // Filter out the versioning table.
            _resetBypass.Add($"`{Journal.Table}`");

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
                await Database.SqlStatement(ReplaceSqlRuntimeParameters(sql)).NonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}