namespace DbEx.Migration;

/// <summary>
/// Encapsulates the <see cref="DbDataReader"/> to provide requisite column value capabilities.
/// </summary>
/// <param name="database">The owning <see cref="IDatabase"/>.</param>
/// <param name="dataReader">The underlying <see cref="DbDataReader"/>.</param>
public class DatabaseRecord(IDatabase database, DbDataReader dataReader)
{
    /// <summary>
    /// Gets the owning <see cref="IDatabase"/>.
    /// </summary>
    public IDatabase Database { get; } = database.ThrowIfNull();

    /// <summary>
    /// Gets the underlying <see cref="DbDataReader"/>.
    /// </summary>
    public DbDataReader DataReader { get; } = dataReader.ThrowIfNull();

    /// <summary>
    /// Gets the named column value.
    /// </summary>
    /// <param name="columnName">The column name.</param>
    /// <returns>The value.</returns>
    public object? GetValue(string columnName) => GetValue(DataReader.GetOrdinal(columnName.ThrowIfNull()));

    /// <summary>
    /// Gets the specified column value.
    /// </summary>
    /// <param name="ordinal">The ordinal index.</param>
    /// <returns>The value.</returns>
    public object? GetValue(int ordinal)
    {
        if (DataReader.IsDBNull(ordinal))
            return default;

        return DataReader.GetValue(ordinal);
    }

    /// <summary>
    /// Gets the named column value.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="columnName">The column name.</param>
    /// <returns>The value.</returns>
    public T? GetValue<T>(string columnName) => GetValue<T>(DataReader.GetOrdinal(columnName.ThrowIfNull()));

    /// <summary>
    /// Gets the specified column value.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="ordinal">The ordinal index.</param>
    /// <returns>The value.</returns>
    public T? GetValue<T>(int ordinal)
    {
        if (DataReader.IsDBNull(ordinal))
            return default!;

#if NET7_0_OR_GREATER
        if (typeof(T) == typeof(Nullable<DateOnly>))
            return (T?)(object)DataReader.GetFieldValue<DateOnly>(ordinal);
        else if (typeof(T) == typeof(Nullable<TimeOnly>))
            return (T?)(object)DataReader.GetFieldValue<TimeOnly>(ordinal);
#endif

        return DataReader.GetFieldValue<T>(ordinal);
    }

    /// <summary>
    /// Indicates whether the named column is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="columnName">The column name.</param>
    /// <param name="ordinal">The corresponding ordinal for the column name.</param>
    /// <returns><see langword="true"/> indicates that the column value has a <see cref="DBNull"/> value; otherwise, <see langword="false"/>.</returns>
    public bool IsDBNull(string columnName, out int ordinal)
    {
        ordinal = DataReader.GetOrdinal(columnName.ThrowIfNull());
        return DataReader.IsDBNull(ordinal);
    }
}