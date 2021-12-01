// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;
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
        Task<string> GenerateStringIdentifierAsync() => throw new NotImplementedException();

        /// <summary>
        /// Generate a new <see cref="Guid"/> identifier.
        /// </summary>
        Task<Guid> GenerateGuidIdentifierAsync() => throw new NotImplementedException();

        /// <summary>
        /// Generate a new <see cref="int"/> identifier.
        /// </summary>
        Task<int> GenerateInt32IdentifierAsync() => throw new NotImplementedException();

        /// <summary>
        /// Generate a new <see cref="long"/> identifier.
        /// </summary>
        Task<long> GenerateInt64IdentifierAsync() => throw new NotImplementedException();
    }
}