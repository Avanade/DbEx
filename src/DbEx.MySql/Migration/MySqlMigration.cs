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
    /// <remarks>The following <see cref="DatabaseMigrationBase.SchemaObjectTypes"/> are supported by default: ''<c>FUNCTION</c>', '<c>VIEW</c>', '<c>PROCEDURE</c>'.
    /// <para>The base <see cref="DatabaseMigrationBase.Journal"/> instance is updated; the <see cref="IDatabaseJournal.Schema"/> and <see cref="IDatabaseJournal.Table"/> properties are set to `<c>null</c>` and `<c>schemaversions</c>` respectively.</para></remarks>
    public class MySqlMigration : DatabaseMigrationBase
    {
        private readonly string _databaseName;
        private readonly IDatabase _database;
        private readonly IDatabase _masterDatabase;
        private readonly List<string> _resetBypass = [];

        /// <summary>
        /// Initializes an instance of the <see cref="MySqlMigration"/> class.
        /// </summary>
        /// <param name="args">The <see cref="MigrationArgsBase"/>.</param>
        public MySqlMigration(MigrationArgsBase args) : base(args)
        {
            SchemaConfig = new MySqlSchemaConfig(this);

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
                SchemaObjectTypes = ["FUNCTION", "VIEW", "PROCEDURE"];

            // Add/set standard parameters.
            Args.AddParameter(MigrationArgsBase.DatabaseNameParamName, _databaseName, true);
            Args.AddParameter(MigrationArgsBase.JournalSchemaParamName, null, true);
            Args.AddParameter(MigrationArgsBase.JournalTableParamName, "schemaversions");
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
        public override DatabaseSchemaConfig SchemaConfig { get; }

        /// <inheritdoc/>
        protected override DatabaseSchemaScriptBase CreateSchemaScript(DatabaseMigrationScript migrationScript) => MySqlSchemaScript.Create(migrationScript);

        /// <inheritdoc/>
        protected override async Task<bool> DatabaseResetAsync(CancellationToken cancellationToken = default)
        {
            // Filter out the versioning table.
            _resetBypass.Add(SchemaConfig.ToFullyQualifiedTableName(Journal.Schema!, Journal.Table!));

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