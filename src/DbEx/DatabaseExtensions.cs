namespace DbEx;

/// <summary>
/// <see cref="IDatabase"/> extensions.
/// </summary>
public static class DatabaseExtensions
{
    private static readonly char[] _snakeCamelCaseSeparatorChars = ['_', '-'];

#if NET6_0_OR_GREATER
    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if the <paramref name="value"/> is <c>null</c>.
    /// </summary>
    /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="value">The value to validate as non-null.</param>
    /// <param name="paramName">The name of the parameter with which the <paramref name="value"/> corresponds.</param>
    /// <returns>The <paramref name="value"/> to support fluent-style method-chaining.</returns>
    internal static T ThrowIfNull<T>([NotNull] this T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        return value;
    }
#else
    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if the <paramref name="value"/> is <c>null</c>.
    /// </summary>
    /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="value">The value to validate as non-null.</param>
    /// <param name="paramName">The name of the parameter with which the <paramref name="value"/> corresponds.</param>
    /// <returns>The <paramref name="value"/> to support fluent-style method-chaining.</returns>
    internal static T ThrowIfNull<T>([NotNull] this T? value, string? paramName = "value")
    {
        if (value is null)
            throw new ArgumentNullException(paramName);
        
        return value;
    }
#endif

#if NET7_0_OR_GREATER
    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if the <paramref name="value"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </summary>
    /// <param name="value">The value to validate as non-null.</param>
    /// <param name="paramName">The name of the parameter with which the <paramref name="value"/> corresponds.</param>
    /// <returns>The <paramref name="value"/> to support fluent-style method-chaining.</returns>
    internal static string ThrowIfNullOrEmpty([NotNull] this string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(value, paramName);
        return value;
    }
#else
    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if the <paramref name="value"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </summary>
    /// <param name="value">The value to validate as non-null.</param>
    /// <param name="paramName">The name of the parameter with which the <paramref name="value"/> corresponds.</param>
    /// <returns>The <paramref name="value"/> to support fluent-style method-chaining.</returns>
    internal static string ThrowIfNullOrEmpty([NotNull] this string? value, string? paramName = "value")
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentNullException(paramName);

        return value;
    }
#endif

    /// <summary>
    /// Performs the specified <paramref name="action"/> on each element in the sequence.
    /// </summary>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    /// <param name="sequence">The sequence to iterate.</param>
    /// <param name="action">The action to perform on each element.</param>
    /// <returns>The sequence.</returns>
    internal static IEnumerable<TItem> ForEach<TItem>(this IEnumerable<TItem> sequence, Action<TItem> action)
    {
        if (sequence == null)
            return sequence!;

        action.ThrowIfNull(nameof(action));

        foreach (TItem element in sequence.ThrowIfNull(nameof(sequence)))
        {
            action(element);
        }

        return sequence;
    }
}