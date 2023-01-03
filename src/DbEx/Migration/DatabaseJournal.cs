// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.Database;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

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
        /// <param name="migrator">The <see cref="DatabaseMigrationBase"/>.</param>
        public DatabaseJournal(DatabaseMigrationBase migrator) => Migrator = migrator ?? throw new ArgumentNullException(nameof(migrator));

        /// <inheritdoc/>
        public string? Schema => Migrator.Args.Parameters[MigrationArgsBase.JournalSchemaParamName]?.ToString();

        /// <inheritdoc/>
        public string? Table => Migrator.Args.Parameters[MigrationArgsBase.JournalTableParamName]?.ToString();

        /// <summary>
        /// Gets the <see cref="DatabaseMigrationBase"/>.
        /// </summary>
        public DatabaseMigrationBase Migrator { get; }

        /// <inheritdoc/>
        public async Task EnsureExistsAsync(CancellationToken cancellationToken = default)
        {
            if (_journalExists)
                return;

            using var sr = DatabaseMigrationBase.GetRequiredResourcesStreamReader($"JournalExists.sql", Migrator.ArtefactResourceAssemblies.ToArray())!;
            var exists = await Migrator.Database.SqlStatement(Migrator.ReplaceSqlRuntimeParameters(sr.ReadToEnd())).ScalarAsync<object?>(cancellationToken).ConfigureAwait(false);
            if (exists != null)
            {
                _journalExists = true;
                return;
            }

            using var sr2 = DatabaseMigrationBase.GetRequiredResourcesStreamReader($"JournalCreate.sql", Migrator.ArtefactResourceAssemblies.ToArray())!;
            await Migrator.Database.SqlStatement(Migrator.ReplaceSqlRuntimeParameters(sr2.ReadToEnd())).NonQueryAsync(cancellationToken).ConfigureAwait(false);

            Migrator.Logger.LogInformation("    *Journal table did not exist within the database and was automatically created.");

            _journalExists = true;
        }

        /// <inheritdoc/>
        public async Task AuditScriptExecutionAsync(DatabaseMigrationScript script, CancellationToken cancellationToken = default)
        {
            await EnsureExistsAsync(cancellationToken).ConfigureAwait(false);

            using var sr = DatabaseMigrationBase.GetRequiredResourcesStreamReader($"JournalAudit.sql", Migrator.ArtefactResourceAssemblies.ToArray())!;
            await Migrator.Database.SqlStatement(Migrator.ReplaceSqlRuntimeParameters(sr.ReadToEnd()))
                .Param("@scriptname", script.Name)
                .Param("@applied", DateTime.UtcNow)
                .NonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetExecutedScriptsAsync(CancellationToken cancellationToken = default)
        {
            await EnsureExistsAsync(cancellationToken).ConfigureAwait(false);

            using var sr = DatabaseMigrationBase.GetRequiredResourcesStreamReader($"JournalPrevious.sql", Migrator.ArtefactResourceAssemblies.ToArray())!;
            return await Migrator.Database.SqlStatement(Migrator.ReplaceSqlRuntimeParameters(sr.ReadToEnd())).SelectQueryAsync(dr => dr.GetValue<string>("scriptname"), cancellationToken).ConfigureAwait(false);
        }
    }
}