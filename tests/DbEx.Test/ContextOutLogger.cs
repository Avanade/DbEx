using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Diagnostics;

namespace DbEx.Test
{
    /// <summary>
    /// Represents the <see cref="TestContextLogger"/> provider.
    /// </summary>
    [ProviderAlias("")]
    [DebuggerStepThrough]
    public sealed class TestContextLoggerProvider : ILoggerProvider
    {
        /// <summary>
        /// Creates a new instance of the <see cref="TestContextLogger"/>.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        /// <returns>The <see cref="TestContextLogger"/>.</returns>
        public ILogger CreateLogger(string name) => new TestContextLogger(name, IncludeLevel, IncludeName);

        /// <summary>
        /// Indicates whether to include the log level in the message.
        /// </summary>
        public bool IncludeLevel { get; set; } = false;

        /// <summary>
        /// Indicates whether to include the logger name in the message.
        /// </summary>
        public bool IncludeName { get; set; } = false;

        /// <summary>
        /// Closes and disposes the <see cref="TestContextLoggerProvider"/>.
        /// </summary>
        public void Dispose() { }
    }

    /// <summary>
    /// Represents a logger where all messages are written directly to <see cref="TestContext.Out"/>.
    /// </summary>
    [DebuggerStepThrough]
    public sealed class TestContextLogger : ILogger
    {
        private readonly string _name;
        private readonly bool _includeName;
        private readonly bool _includeLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestContextLogger"/> class.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        /// <param name="includeName">Indicates whether to include the logger name in the message.</param>
        public TestContextLogger(string name, bool includeLevel, bool includeName)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _includeLevel = includeLevel;
            _includeName = includeName;
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state) => NullScope.Default;

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            var message = formatter(state, exception);
            if (_includeLevel)
                message = $"{GetLogLevel(logLevel)}: {message}";

            if (_includeName)
                message += $" [{_name}]";

            if (exception != null)
                message += Environment.NewLine + exception;

            TestContext.Out.WriteLine(message);
        }

        /// <summary>
        /// Gets the shortened log level.
        /// </summary>
        internal static string GetLogLevel(LogLevel level) =>
            level switch
            {
                LogLevel.Critical => "cri",
                LogLevel.Error => "err",
                LogLevel.Warning => "wrn",
                LogLevel.Information => "inf",
                LogLevel.Debug => "dbg",
                LogLevel.Trace => "trc",
                _ => "?",
            };

        /// <summary>
        /// Represents a null scope for loggers.
        /// </summary>
        private sealed class NullScope : IDisposable
        {
            /// <summary>
            /// Gets the default instance.
            /// </summary>
            public static NullScope Default { get; } = new NullScope();

            /// <summary>
            /// Initializes a new instance of the <see cref="NullScope"/> class.
            /// </summary>
            private NullScope() { }

            /// <summary>
            /// Closes and disposes the <see cref="NullScope"/>.
            /// </summary>
            public void Dispose() { }
        }
    }
}