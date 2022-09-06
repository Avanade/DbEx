// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;

namespace DbEx.Console
{
    /// <summary>
    /// Represents the base capabilities for the database migration orchestration.
    /// </summary>
    public abstract class MigratorConsoleBase<T> : MigratorConsoleBase where T : MigratorConsoleBase<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MigratorConsoleBase"/> class.
        /// </summary>
        /// <param name="args">The default <see cref="MigratorConsoleArgs"/> that will be overridden/updated by the command-line argument values.</param>
        protected MigratorConsoleBase(MigratorConsoleArgs? args = null) : base(args) { }

        /// <summary>
        /// Access the underlying <see cref="MigratorConsoleBase.Args"/>.
        /// </summary>
        /// <param name="action">The action to invoke to access the <see cref="MigratorConsoleBase.Args"/>.</param>
        /// <returns>The current <see cref="MigratorConsoleBase{T}"/> instance to support fluent-style method-chaining.</returns>
        public T ConsoleArgs(Action<MigratorConsoleArgs> action)
        {
            action?.Invoke(Args);
            return (T)this;
        }
    }
}