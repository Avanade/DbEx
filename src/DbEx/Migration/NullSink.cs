// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbUp.Engine.Output;

namespace DbEx.Migration
{
    /// <summary>
    /// Represents a <i>DbUp</i> <see cref="IUpgradeLog"/> <c>null</c> sink; i.e. all messages to be swallowed.
    /// </summary>
    public class NullSink : IUpgradeLog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullSink"/> class.
        /// </summary>
        public NullSink() { }

        /// <summary>
        /// Writes/logs an error message.
        /// </summary>
        public void WriteError(string format, params object[] args) { }

        /// <summary>
        /// Writes/logs an informational message.
        /// </summary>
        public void WriteInformation(string format, params object[] args) { }

        /// <summary>
        /// Writes/logs a warning message.
        /// </summary>
        public void WriteWarning(string format, params object[] args) { }
    }
}