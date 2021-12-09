// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.Schema;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace DbEx
{
    /// <summary>
    /// Defines the database access.
    /// </summary>
    public interface IDatabase : IDisposable
    {
        /// <summary>
        /// Gets the <see cref="DbConnection"/>.
        /// </summary>
        /// <remarks>The connection is created and opened on first use, and closed on <see cref="IDisposable.Dispose()"/>.</remarks>
        Task<DbConnection> GetConnectionAsync();

        /// <summary>
        /// Creates a stored procedure <see cref="DatabaseCommand"/>.
        /// </summary>
        /// <param name="storedProcedure">The stored procedure name.</param>
        /// <param name="parameters">An optional delegate to update the <see cref="DatabaseParameterCollection"/> for the command.</param>
        /// <returns>The <see cref="DatabaseCommand"/>.</returns>
        DatabaseCommand StoredProcedure(string storedProcedure, Action<DatabaseParameterCollection>? parameters = null);

        /// <summary>
        /// Creates a SQL statement <see cref="DatabaseCommand"/>.
        /// </summary>
        /// <param name="sqlStatement">The SQL statement.</param>
        /// <param name="parameters">An optional delegate to update the <see cref="DatabaseParameterCollection"/> for the command.</param>
        /// <returns>The <see cref="DatabaseCommand"/>.</returns>
        DatabaseCommand SqlStatement(string sqlStatement, Action<DatabaseParameterCollection>? parameters = null);

        /// <summary>
        /// Invoked where a <see cref="DbException"/> has been thrown.
        /// </summary>
        /// <param name="dbex">The <see cref="DbException"/>.</param>
        /// <remarks>Provides an opportunity to inspect and handle the exception before it bubbles up.</remarks>
        void OnDbException(DbException dbex);

        /// <summary>
        /// Selects all the table and column schema details from the database.
        /// </summary>
        /// <param name="refDataPredicate">The reference data predicate used to determine whether a <see cref="DbTableSchema"/> is considered a reference data table (sets <see cref="DbTableSchema.IsRefData"/>).</param>
        /// <returns>A list of all the table and column schema details.</returns>
        Task<List<DbTableSchema>> SelectSchemaAsync(Func<DbTableSchema, bool>? refDataPredicate = null);
    }
}