// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.Database;
using Microsoft.Extensions.Logging;
using OnRamp.Utility;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DbEx.Migration.SqlServer
{
    /// <summary>
    /// Provides the <see href="https://docs.microsoft.com/en-us/sql/connect/ado-net/microsoft-ado-net-sql-server">SQL Server</see> journaling capability to ensure selected scripts are only executed once.
    /// </summary>
    public class SqlServerJournal : IDatabaseJournal
    {
        private bool _journalExists;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerJournal"/> class.
        /// </summary>
        /// <param name="database">The <see cref="IDatabase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public SqlServerJournal(IDatabase database, ILogger logger)
        {
            Database = database ?? throw new ArgumentNullException(nameof(database));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the <see cref="IDatabase"/>.
        /// </summary>
        public IDatabase Database { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        public ILogger Logger { get; }

        /// <inheritdoc/>
        public async Task EnsureExistsAsync(CancellationToken cancellationToken = default)
        {
            if (_journalExists)
                return;

            using var sr = StreamLocator.GetResourcesStreamReader("SqlServer.JournalEnsureExists.sql", new Assembly[] { typeof(SqlServerJournal).Assembly }).StreamReader!;
            var message = await Database.SqlStatement(sr.ReadToEnd()).ScalarAsync<string?>(cancellationToken).ConfigureAwait(false);
            if (message is not null)
                Logger.LogInformation("    {Content}", message);

            _journalExists = true;
        }

        /// <inheritdoc/>
        public async Task AuditScriptExecutionAsync(DatabaseMigrationScript script, CancellationToken cancellationToken = default)
        {
            await EnsureExistsAsync(cancellationToken).ConfigureAwait(false);

            using var sr = StreamLocator.GetResourcesStreamReader("SqlServer.JournalAuditScript.sql", new Assembly[] { typeof(SqlServerJournal).Assembly }).StreamReader!;
            await Database.SqlStatement(sr.ReadToEnd())
                .Param("@scriptName", script.Name)
                .Param("@applied", DateTime.UtcNow)
                .NonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetExecutedScriptsAsync(CancellationToken cancellationToken = default)
        {
            await EnsureExistsAsync(cancellationToken).ConfigureAwait(false);

            using var sr = StreamLocator.GetResourcesStreamReader("SqlServer.JournalGetExecuted.sql", new Assembly[] { typeof(SqlServerJournal).Assembly }).StreamReader!;
            return await Database.SqlStatement(sr.ReadToEnd()).SelectQueryAsync(dr => dr.GetValue<string>("ScriptName"), cancellationToken).ConfigureAwait(false);
        }
    }
}