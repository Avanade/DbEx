// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System.Threading.Tasks;

namespace DbEx.Console
{
    /// <summary>
    /// Provides the direct console capabilities.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// The main entry point.
        /// </summary>
        /// <param name="args">The console arguments.</param>
        /// <returns><b>Zero</b> indicates success; otherwise, unsuccessful.</returns>
        internal static async Task<int> Main(string[] args) => await Task.FromResult(0).ConfigureAwait(false);
    }
}