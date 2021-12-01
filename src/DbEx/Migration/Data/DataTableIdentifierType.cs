// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;

namespace DbEx.Migration.Data
{
    /// <summary>
    /// Defines the identifier generator <see cref="Type"/>.
    /// </summary>
    public enum DataTableIdentifierType
    {
        /// <summary>
        /// Represents an invalid <see cref="Type"/>.
        /// </summary>
        Invalid = 0,

        /// <summary>
        /// Represents a <see cref="string"/> <see cref="Type"/>.
        /// </summary>
        String = 1,

        /// <summary>
        /// Represents a <see cref="System.Guid"/> <see cref="Type"/>.
        /// </summary>
        Guid = 2,

        /// <summary>
        /// Represents an <see cref="int"/> <see cref="Type"/>.
        /// </summary>
        Int = 3,

        /// <summary>
        /// Represents a <see cref="long"/> <see cref="Type"/>.
        /// </summary>
        Long = 4
    }
}