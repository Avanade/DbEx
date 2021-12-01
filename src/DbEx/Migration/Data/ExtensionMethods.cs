// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;
using System.Diagnostics;

namespace DbEx.Migration.Data
{
    /// <summary>
    /// Provides extension methods.
    /// </summary>
    [DebuggerStepThrough]
    public static class ExtensionMethods
    {
        /// <summary>
        /// Converts an <see cref="int"/> to a <see cref="Guid"/>; e.g. '<c>1</c>' will result in '<c>00000001-0000-0000-0000-000000000000</c>'.
        /// </summary>
        /// <param name="value">The <see cref="int"/> value.</param>
        /// <returns>The corresponding <see cref="Guid"/>.</returns>
        /// <remarks>Sets the first argument with the <paramref name="value"/> and the remainder with zeroes using <see cref="Guid(int, short, short, byte[])"/>.</remarks>
        public static Guid ToGuid(this int value) => new(value, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }
}