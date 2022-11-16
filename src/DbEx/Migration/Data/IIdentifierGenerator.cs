// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;
using System.Threading;
using System.Threading.Tasks;

namespace DbEx.Migration.Data
{
    /// <summary>
    /// Enables the generation of a new identifier value.
    /// </summary>
    public interface IIdentifierGenerator
    {
        /// <summary>
        /// Generate a new <see cref="string"/> identifier.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        Task<string> GenerateStringIdentifierAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        /// <summary>
        /// Generate a new <see cref="Guid"/> identifier.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        Task<Guid> GenerateGuidIdentifierAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        /// <summary>
        /// Generate a new <see cref="int"/> identifier.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        Task<int> GenerateInt32IdentifierAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        /// <summary>
        /// Generate a new <see cref="long"/> identifier.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        Task<long> GenerateInt64IdentifierAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}