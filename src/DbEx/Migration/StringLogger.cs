// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace DbEx.Migration
{
    /// <summary>
    /// Provides a basic <see cref="ILogger"/> that captures the <see cref="Output"/> as a <see cref="string"/>.
    /// </summary>
    internal class StringLogger : ILogger
    {
        private readonly StringBuilder _stringBuilder = new();
        private readonly LoggerExternalScopeProvider _scopeProvider = new();

        /// <inheritdoc/>
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => _scopeProvider.Push(state);

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <inheritdoc/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => _stringBuilder.AppendLine(formatter(state, exception));

        /// <summary>
        /// Gets the log output.
        /// </summary>
        public string Output => _stringBuilder.ToString();
    }
}