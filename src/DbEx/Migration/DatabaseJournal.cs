// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.Database;
using OnRamp.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbEx.Migration
{
    /// <summary>
    /// Provides the database-agnostic journaling capability to ensure selected scripts are only executed once.
    /// </summary>
    /// <remarks>Journaling is the recording/auditing of migration scripts executed against the database to ensure they are executed only once. This is implemented in a manner compatible, same-as, 
    /// <see href="https://github.com/DbUp/DbUp/blob/master/src/dbup-core/Support/TableJournal.cs">DbUp</see> to ensure consistency.
    /// <para>The <see cref="Schema"/> and <see cref="Table"/> values are used to replace the '<c>{{JournalSchema}}</c>' and '<c>{{JournalTable}}</c>' placeholders respectively.</para></remarks>
    public class DatabaseJournal : IDatabaseJournal
    {
        private bool _journalExists;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseJournal"/> class.
        /// </summary>
        /// <param name="migrator">The <see cref="DatabaseMigratorBase"/>.</param>
        public DatabaseJournal(DatabaseMigratorBase migrator) => Migrator = migrator ?? throw new ArgumentNullException(nameof(migrator));

        /// <inheritdoc/>
        public string? Schema { get; set; }

        /// <inheritdoc/>
        public string? Table { get; set; }

        /// <summary>
        /// Gets the <see cref="DatabaseMigratorBase"/>.
        /// </summary>
        public DatabaseMigratorBase Migrator { get; }

        /// <inheritdoc/>
        public async Task EnsureExistsAsync(CancellationToken cancellationToken = default)
        {
            if (_journalExists)
                return;

            using var sr = StreamLocator.GetResourcesStreamReader($"{Migrator.Provider}.JournalExists.sql", Migrator.ArtefactResourceAssemblies).StreamReader!;
            var exists = await Migrator.Database.SqlStatement(ReplacePlacholders(sr.ReadToEnd())).ScalarAsync<int?>(cancellationToken).ConfigureAwait(false);
            if (exists.HasValue && exists.Value == 1)
            {
                _journalExists = true;
                return;
            }

            using var sr2 = StreamLocator.GetResourcesStreamReader($"{Migrator.Provider}.JournalCreate.sql", Migrator.ArtefactResourceAssemblies).StreamReader!;
            await Migrator.Database.SqlStatement(ReplacePlacholders(sr2.ReadToEnd())).NonQueryAsync(cancellationToken).ConfigureAwait(false);

            Migrator.Logger.LogInformation("    *Journal table did not exist within the database and was automatically created.");

            _journalExists = true;
        }

        /// <inheritdoc/>
        public async Task AuditScriptExecutionAsync(DatabaseMigrationScript script, CancellationToken cancellationToken = default)
        {
            await EnsureExistsAsync(cancellationToken).ConfigureAwait(false);

            using var sr = StreamLocator.GetResourcesStreamReader($"{Migrator.Provider}.JournalAudit.sql", Migrator.ArtefactResourceAssemblies).StreamReader!;
            await Migrator.Database.SqlStatement(ReplacePlacholders(sr.ReadToEnd()))
                .Param("@scriptname", script.Name)
                .Param("@applied", DateTime.UtcNow)
                .NonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetExecutedScriptsAsync(CancellationToken cancellationToken = default)
        {
            await EnsureExistsAsync(cancellationToken).ConfigureAwait(false);

            using var sr = StreamLocator.GetResourcesStreamReader($"{Migrator.Provider}.JournalPrevious.sql", Migrator.ArtefactResourceAssemblies).StreamReader!;
            return await Migrator.Database.SqlStatement(ReplacePlacholders(sr.ReadToEnd())).SelectQueryAsync(dr => dr.GetValue<string>("scriptname"), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Replace the placeholders.
        /// </summary>
        private string ReplacePlacholders(string sql) => string.IsNullOrEmpty(sql) ? sql : sql.Replace("{{JournalSchema}}", Schema).Replace("{{JournalTable}}", Table);
    }
}