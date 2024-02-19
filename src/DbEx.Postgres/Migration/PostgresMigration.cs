// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.Database;
using CoreEx.Database.Postgres;
using DbEx.DbSchema;
using DbEx.Migration;
using DbUp.Support;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbEx.Postgres.Migration
{
    /// <summary>
    /// Provides the <see href="">PostgreSQL</see> migration orchestration.
    /// </summary>
    public class PostgresMigration : DatabaseMigrationBase
    {
        private readonly string _databaseName;
        private readonly IDatabase _database;
        private readonly IDatabase _masterDatabase;
        private readonly List<string> _resetBypass = [];

        /// <summary>
        /// Initializes an instance of the <see cref="PostgresMigration"/> class.
        /// </summary>
        /// <param name="args">The <see cref="MigrationArgsBase"/>.</param>
        public PostgresMigration(MigrationArgsBase args) : base(args)
        {
            var csb = new NpgsqlConnectionStringBuilder(Args.ConnectionString);
            if (string.IsNullOrEmpty(csb.Database))
                throw new ArgumentException($"The {nameof(OnRamp.CodeGeneratorDbArgsBase.ConnectionString)} property must contain a database name.", nameof(args));

            _databaseName = csb.Database;
            _database = new PostgresDatabase(() => new NpgsqlConnection(Args.ConnectionString));

            csb.Database = null;
            _masterDatabase = new PostgresDatabase(() => new NpgsqlConnection(csb.ConnectionString));

            Args.AddAssemblyAfter(typeof(DatabaseMigrationBase).Assembly, typeof(PostgresMigration).Assembly);

            if (SchemaObjectTypes.Length == 0)
                SchemaObjectTypes = ["FUNCTION", "VIEW", "PROCEDURE"];

            Args.Parameter(MigrationArgsBase.DatabaseNameParamName, _databaseName, true);
            Args.Parameter(MigrationArgsBase.JournalSchemaParamName, DatabaseSchemaConfig.DefaultSchema, true);
            Args.Parameter(MigrationArgsBase.JournalTableParamName, "schemaversions");
        }

        /// <inheritdoc/>
        public override string Provider => "Postgres";

        /// <inheritdoc/>
        public override string DatabaseName => _databaseName;

        /// <inheritdoc/>
        public override IDatabase Database => _database;

        /// <inheritdoc/>
        public override IDatabase MasterDatabase => _masterDatabase;

        /// <inheritdoc/>
        public override DatabaseSchemaConfig DatabaseSchemaConfig => new PostgresSchemaConfig(DatabaseName);

        /// <inheritdoc/>
        protected override DatabaseSchemaScriptBase CreateSchemaScript(DatabaseMigrationScript migrationScript) => PostgresSchemaScript.Create(migrationScript);

        /// <inheritdoc/>
        protected override async Task<bool> DatabaseResetAsync(CancellationToken cancellationToken = default)
        {
            // Filter out the versioning table.
            _resetBypass.Add(DatabaseSchemaConfig.ToFullyQualifiedTableName(Journal.Schema!, Journal.Table!));

            // Carry on as they say ;-)
            return await base.DatabaseResetAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override Func<DbTableSchema, bool> DataResetFilterPredicate =>
            schema => !_resetBypass.Contains(schema.QualifiedName!) && !schema.Name.StartsWith("pg_");

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