// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.Database;
using OnRamp.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DbEx.Migration
{
    /// <summary>
    /// Provides the database-agnostic journaling capability to ensure selected scripts are only executed once.
    /// </summary>
    /// <remarks>Journaling is the recording/auditing of migration scripts executed against the database to ensure they are executed only once. This is implemented in a manner compatible, same-as, 
    /// <see href="https://github.com/DbUp/DbUp/blob/master/src/dbup-core/Support/TableJournal.cs">DbUp</see> to ensure consistency.</remarks>
    public class DatabaseJournal : IDatabaseJournal
    {
        private bool _journalExists;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseJournal"/> class.
        /// </summary>
        /// <param name="migrator">The <see cref="DatabaseMigratorBase"/>.</param>
        public DatabaseJournal(DatabaseMigratorBase migrator)
        {
            Migrator = migrator ?? throw new ArgumentNullException(nameof(migrator));

            JournalExistsResourceName = $"{Migrator.Provider}.JournalExists.sql";
            JournalCreateResourceName = $"{Migrator.Provider}.JournalCreate.sql";
            JournalAuditResourceName = $"{Migrator.Provider}.JournalAudit.sql";
            JournalPreviousResourceName = $"{Migrator.Provider}.JournalPrevious.sql";
        }

        /// <summary>
        /// Gets the <see cref="DatabaseMigratorBase"/>.
        /// </summary>
        public DatabaseMigratorBase Migrator { get; }

        /// <summary>
        /// Gets the resource name for the journal exists SQL.
        /// </summary>
        protected virtual string JournalExistsResourceName { get; private set; }

        /// <summary>
        /// Gets the resource name for the journal create SQL.
        /// </summary>
        protected virtual string JournalCreateResourceName { get; private set; }

        /// <summary>
        /// Gets the resource name for the journal audit SQL.
        /// </summary>
        protected virtual string JournalAuditResourceName { get; private set; }

        /// <summary>
        /// Gets the resource name for the journal previous SQL.
        /// </summary>
        protected virtual string JournalPreviousResourceName { get; private set; }

        /// <inheritdoc/>
        public async Task EnsureExistsAsync(CancellationToken cancellationToken = default)
        {
            if (_journalExists)
                return;

            using var sr = StreamLocator.GetResourcesStreamReader(JournalExistsResourceName, new Assembly[] { GetType().Assembly }).StreamReader!;
            var exists = await Migrator.Database.SqlStatement(sr.ReadToEnd()).ScalarAsync<int?>(cancellationToken).ConfigureAwait(false);
            if (exists.HasValue && exists.Value == 1)
            {
                _journalExists = true;
                return;
            }

            using var sr2 = StreamLocator.GetResourcesStreamReader(JournalCreateResourceName, new Assembly[] { GetType().Assembly }).StreamReader!;
            await Migrator.Database.SqlStatement(sr2.ReadToEnd()).NonQueryAsync(cancellationToken).ConfigureAwait(false);

            Migrator.Logger.LogInformation("    *Journal table did not exist within the database and was automatically created.'");

            _journalExists = true;
        }

        /// <inheritdoc/>
        public async Task AuditScriptExecutionAsync(DatabaseMigrationScript script, CancellationToken cancellationToken = default)
        {
            await EnsureExistsAsync(cancellationToken).ConfigureAwait(false);

            using var sr = StreamLocator.GetResourcesStreamReader(JournalAuditResourceName, new Assembly[] { GetType().Assembly }).StreamReader!;
            await Migrator.Database.SqlStatement(sr.ReadToEnd())
                .Param("@scriptName", script.Name)
                .Param("@applied", DateTime.UtcNow)
                .NonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetExecutedScriptsAsync(CancellationToken cancellationToken = default)
        {
            await EnsureExistsAsync(cancellationToken).ConfigureAwait(false);

            using var sr = StreamLocator.GetResourcesStreamReader(JournalPreviousResourceName, new Assembly[] { GetType().Assembly }).StreamReader!;
            return await Migrator.Database.SqlStatement(sr.ReadToEnd()).SelectQueryAsync(dr => dr.GetValue<string>("ScriptName"), cancellationToken).ConfigureAwait(false);
        }
    }
}