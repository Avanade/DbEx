// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DbEx.Migration
{
    /// <summary>
    /// Enables the database journaling capability to ensure selected scripts are only executed once.
    /// </summary>
    public interface IDatabaseJournal
    {
        /// <summary>
        /// Gets the journal schema name.
        /// </summary>
        string? Schema { get; }

        /// <summary>
        /// Gets the journal table name.
        /// </summary>
        string? Table { get; }

        /// <summary>
        /// Ensures that the <see cref="IDatabaseJournal"/> exists within the database.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        Task EnsureExistsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the list of all the previously executed scripts.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The list of all the previously executed scripts.</returns>
        Task<IEnumerable<string>> GetExecutedScriptsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Audits (persists) the execution of a <paramref name="script"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="script">The <see cref="DatabaseMigrationScript"/>.</param>
        Task AuditScriptExecutionAsync(DatabaseMigrationScript script, CancellationToken cancellationToken = default);
    }
}