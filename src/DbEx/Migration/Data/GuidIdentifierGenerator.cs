// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;
using System.Threading.Tasks;

namespace DbEx.Migration.Data
{
    /// <summary>
    /// Represents a <see cref="Guid"/>-based generator that will return a <see cref="Guid.NewGuid"/> for <see cref="IIdentifierGenerator.GenerateGuidIdentifierAsync"/> and <see cref="IIdentifierGenerator.GenerateStringIdentifierAsync"/>.
    /// </summary>
    public class GuidIdentifierGenerator : IIdentifierGenerator
    {
        /// <summary>
        /// Generate a new <see cref="string"/> identifier.
        /// </summary>
        public Task<string> GenerateStringIdentifierAsync() => Task.FromResult(Guid.NewGuid().ToString());

        /// <summary>
        /// Generate a new <see cref="Guid"/> identifier.
        /// </summary>
        public Task<Guid> GenerateGuidIdentifierAsync() => Task.FromResult(Guid.NewGuid());
    }
}