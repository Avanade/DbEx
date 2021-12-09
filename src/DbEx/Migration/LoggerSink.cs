// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbUp.Engine.Output;
using Microsoft.Extensions.Logging;
using System;

namespace DbEx.Migration
{
    /// <summary>
    /// Represents a <i>DbUp</i> <see cref="IUpgradeLog"/> to <see cref="ILogger"/> sink.
    /// </summary>
    public class LoggerSink : IUpgradeLog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerSink"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public LoggerSink(ILogger logger) => Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        /// <summary>
        /// Gets the underlying <see cref="ILogger"/>.
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Writes/logs an error message.
        /// </summary>
        public void WriteError(string format, params object[] args) { }

        /// <summary>
        /// Writes/logs an informational message.
        /// </summary>
        public void WriteInformation(string format, params object[] args) => Logger.LogInformation($"    {format}", args);

        /// <summary>
        /// Writes/logs a warning message.
        /// </summary>
        public void WriteWarning(string format, params object[] args) => Logger.LogWarning($"    {format}", args);
    }
}