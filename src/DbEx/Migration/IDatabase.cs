using DbEx.DbSchema;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace DbEx.Migration
{
    /// <summary>
    /// Enables database (relational) access.
    /// </summary>
    public interface IDatabase : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Gets the <see cref="DbProviderFactory"/>.
        /// </summary>
        DbProviderFactory Provider { get; }

        /// <summary>
        /// Gets the <see cref="DbConnection"/>.
        /// </summary>
        /// <remarks>The connection is created and opened on first use, and closed on <see cref="IAsyncDisposable.DisposeAsync()"/> or <see cref="IDisposable.Dispose()"/>.</remarks>
        DbConnection GetConnection();

        /// <summary>
        /// Gets the <see cref="DbConnection"/>.
        /// </summary>
        /// <remarks>The connection is created and opened on first use, and closed on <see cref="IAsyncDisposable.DisposeAsync()"/> or <see cref="IDisposable.Dispose()"/>.</remarks>
        Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a stored procedure <see cref="DatabaseCommand"/>.
        /// </summary>
        /// <param name="storedProcedure">The stored procedure name.</param>
        /// <returns>The <see cref="DatabaseCommand"/>.</returns>
        DatabaseCommand StoredProcedure(string storedProcedure);

        /// <summary>
        /// Creates a SQL statement <see cref="DatabaseCommand"/>.
        /// </summary>
        /// <param name="sqlStatement">The SQL statement.</param>
        /// <returns>The <see cref="DatabaseCommand"/>.</returns>
        DatabaseCommand SqlStatement(string sqlStatement);

        /// <summary>
        /// Selects all the table and column schema details from the database.
        /// </summary>
        /// <param name="migration">The <see cref="DatabaseMigrationBase"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A list of all the table and column schema details.</returns>
        Task<List<DbTableSchema>> SelectSchemaAsync(DatabaseMigrationBase migration, CancellationToken cancellationToken = default);
    }
}