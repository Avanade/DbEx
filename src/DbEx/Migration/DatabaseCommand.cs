namespace DbEx.Migration;

/// <summary>
/// Provides extended database command capabilities.
/// </summary>
/// <param name="db">The <see cref="IDatabase"/>.</param>
/// <param name="commandType">The <see cref="System.Data.CommandType"/>.</param>
/// <param name="commandText">The command text.</param>
/// <remarks>As the underlying <see cref="DbCommand"/> implements <see cref="IDisposable"/> this is only created (and automatically disposed) where executing the command proper.</remarks>
public class DatabaseCommand(IDatabase db, System.Data.CommandType commandType, string commandText)
{
    private readonly List<DbParameter> _parameters = [];

    /// <summary>
    /// Gets the underlying <see cref="IDatabase"/>.
    /// </summary>
    public IDatabase Database { get; } = db.ThrowIfNull();

    /// <summary>
    /// Gets the <see cref="System.Data.CommandType"/>.
    /// </summary>
    public System.Data.CommandType CommandType { get; } = commandType;

    /// <summary>
    /// Gets the command text.
    /// </summary>
    public string CommandText { get; } = commandText.ThrowIfNullOrEmpty();

    /// <summary>
    /// Adds a parameter to the command.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <returns>The <see cref="DatabaseCommand"/>.</returns>
    public DatabaseCommand Param<T>(string name, T? value = default)
    {
        var param = Database.Provider.CreateParameter() ?? throw new InvalidOperationException($"The {nameof(DbProviderFactory)}.{nameof(DbProviderFactory.CreateParameter)} returned a null.");
        param.ParameterName = name;

        // Convert to UTC. https://www.tinybird.co/blog/database-timestamps-timezone
        param.Value = value is null 
            ? DBNull.Value 
            : (value is DateTimeOffset dto ? dto.ToUniversalTime() : value);

        _parameters.Add(param);
        return this;
    }

    /// <summary>
    /// Selects none or more items from the first result set.
    /// </summary>
    /// <typeparam name="T">The item <see cref="Type"/>.</typeparam>
    /// <param name="func">The <see cref="DatabaseRecord"/> mapping function.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting set.</returns>
    public async Task<IEnumerable<T>> SelectQueryAsync<T>(Func<DatabaseRecord, T> func, CancellationToken cancellationToken = default)
    {
        func.ThrowIfNull(nameof(func));

        var list = new List<T>();

        using var cmd = await CreateDbCommandAsync(cancellationToken).ConfigureAwait(false);
        using var dr = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(func(new DatabaseRecord(Database, dr)));
        }

        return list;
    }

    /// <summary>
    /// Executes the query and returns the first column of the first row in the result set returned by the query.
    /// </summary>
    /// <typeparam name="T">The result <see cref="Type"/>.</typeparam>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The value of the first column of the first row in the result set.</returns>
    public async Task<T> ScalarAsync<T>(CancellationToken cancellationToken = default)
    {
        using var cmd = await CreateDbCommandAsync(cancellationToken).ConfigureAwait(false);
        var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        if (typeof(T) == typeof(DateTimeOffset) && result is DateTime dt)
        {
            // https://www.tinybird.co/blog/database-timestamps-timezone
            var dto = dt.Kind switch
            {
                DateTimeKind.Utc => new DateTimeOffset(dt, TimeSpan.Zero),
                DateTimeKind.Local => new DateTimeOffset(dt),
                _ =>throw new InvalidOperationException($"{nameof(DateTime)} with {nameof(DateTime.Kind)} of {dt.Kind} cannot be safely converted to a {nameof(DateTimeOffset)}.")
            };

            return (T)(object)dto;
        }
        else
            return result is null ? default! : result is DBNull ? default! : (T)result;
    }

    /// <summary>
    /// Executes a non-query command.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The number of rows affected.</returns>
    public async Task<int> NonQueryAsync(CancellationToken cancellationToken = default)
    {
        using var cmd = await CreateDbCommandAsync(cancellationToken).ConfigureAwait(false);
        return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates the corresponding <see cref="DbCommand"/>.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DbCommand"/>.</returns>
    private async Task<DbCommand> CreateDbCommandAsync(CancellationToken cancellationToken = default)
    {
        var cmd = (await Database.GetConnectionAsync(cancellationToken).ConfigureAwait(false)).CreateCommand();
        cmd.CommandType = CommandType;
        cmd.CommandText = CommandText;
        cmd.Parameters.AddRange(_parameters.ToArray());
        return cmd;
    }
}