// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;

namespace DbEx.Migration.Data
{
    /// <summary>
    /// Represents a <see cref="DataParser"/> exception.
    /// </summary>
    public class DataParserException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataParserException"/> class.
        /// </summary>
        public DataParserException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataParserException"/> class with a specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message.</param>
        public DataParserException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataParserException"/> class with a specified <paramref name="message"/> and <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner <see cref="Exception"/>.</param>
        public DataParserException(string message, Exception innerException) : base(message, innerException) { }
    }
}