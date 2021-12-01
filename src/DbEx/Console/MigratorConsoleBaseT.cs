// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;
using System.Reflection;

namespace DbEx.Console
{
    /// <summary>
    /// Represents the base capabilities for the database migration orchestrator leveraging <see href="https://dbup.readthedocs.io/en/latest/">DbUp</see>.
    /// </summary>
    public abstract class MigratorConsoleBase<T> : MigratorConsoleBase where T : MigratorConsoleBase<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MigratorConsoleBase"/> class.
        /// </summary>
        /// <param name="name">The application/command name.</param>
        /// <param name="text">The application/command short text.</param>
        /// <param name="description">The application/command description; will default to <paramref name="text"/> when not specified.</param>
        /// <param name="version">The application/command version number.</param>
        /// <param name="args">The default <see cref="MigratorConsoleArgs"/> that will be overridden/updated by the command-line argument values.</param>
        protected MigratorConsoleBase(string name, string text, string? description = null, string? version = null, MigratorConsoleArgs? args = null)
            : base(name, text, description, version, args) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MigratorConsoleBase"/> class defaulting <see cref="MigratorConsoleBase.Name"/> (with <see cref="AssemblyName.Name"/>), <see cref="MigratorConsoleBase.Text"/> (with <see cref="AssemblyProductAttribute.Product"/>),
        /// <see cref="MigratorConsoleBase.Description"/> (with <see cref="AssemblyDescriptionAttribute.Description"/>), and <see cref="Version"/> (with <see cref="AssemblyName.Version"/>) from the <paramref name="assembly"/> where not expressly provided.
        /// </summary>
        /// <param name="args">The default <see cref="MigratorConsoleArgs"/> that will be overridden/updated by the command-line argument values.</param>
        /// <param name="assembly">The <see cref="Assembly"/> to infer properties where not expressly provided.</param>
        /// <param name="name">The application/command name; defaults to <see cref="AssemblyName.Name"/>.</param>
        /// <param name="text">The application/command short text.</param>
        /// <param name="description">The application/command description; defaults to <paramref name="text"/> when not specified.</param>
        /// <param name="version">The application/command version number.</param>
        protected MigratorConsoleBase(Assembly assembly, MigratorConsoleArgs? args = null, string? name = null, string? text = null, string? description = null, string? version = null)
            : base(assembly, args, name, text, description, version) { }

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