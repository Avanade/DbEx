// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.Migration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DbEx.Console
{
    /// <summary>
    /// Base console that facilitates the <see cref="DatabaseMigrationBase"/> by managing the standard console command-line arguments/options.
    /// </summary>
    /// <typeparam name="TSelf">The <see cref="Type"/> itself being implemented.</typeparam>
    public abstract class MigrationConsoleBase<TSelf> : MigrationConsoleBase where TSelf : MigrationConsoleBase<TSelf>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationConsoleBase"/> class.
        /// </summary>
        /// <param name="args">The default <see cref="MigrationArgs"/> that will be overridden/updated by the command-line argument values.</param>
        protected MigrationConsoleBase(MigrationArgsBase args) : base(args) { }

        /// <summary>
        /// Enables fluent-style method-chaining configuration of <typeparamref name="TSelf"/>
        /// </summary>
        /// <param name="action">The action to invoke to access the <see cref="MigrationConsoleBase.Args"/>.</param>
        /// <returns>The current <see cref="MigrationConsoleBase{T}"/> instance to support fluent-style method-chaining.</returns>
        public TSelf Configure(Action<TSelf> action)
        {
            action?.Invoke((TSelf)this);
            return (TSelf)this;
        }

        /// <summary>
        /// Adds the <paramref name="assemblies"/> containing the embedded resources (shortcut to the <see cref="MigrationConsoleBase.Args"/> <see cref="MigrationArgsBase.Assemblies"/>.)
        /// </summary>
        /// <param name="assemblies">The assemblies containing the embedded resources.</param>
        /// <returns>The current instance to supported fluent-style method-chaining.</returns>
        public TSelf Assemblies(params Assembly[] assemblies)
        {
            Args.AddAssembly(assemblies);
            return (TSelf)this;
        }

        /// <summary>
        /// Adds the <paramref name="types"/> containing the embedded resources (shortcut to the <see cref="MigrationConsoleBase.Args"/> <see cref="MigrationArgsBase.Assemblies"/>.)
        /// </summary>
        /// <param name="types">The types to add (infers underlying <see cref="System.Reflection.Assembly"/>).</param>
        /// <returns>The current instance to supported fluent-style method-chaining.</returns>
        public TSelf Assemblies(params Type[] types)
        {
            var list = new List<Assembly>();
            foreach (var t in types)
            {
                list.Add(t.Assembly);
            }

            Args.AddAssembly([.. list]);
            return (TSelf)this;
        }

        /// <summary>
        /// Adds the <typeparamref name="T"/> <see cref="System.Reflection.Assembly"/> containing the embedded resources (shortcut to the <see cref="MigrationConsoleBase.Args"/> <see cref="MigrationArgsBase.Assemblies"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> to infer <see cref="System.Reflection.Assembly"/>.</typeparam>
        /// <returns>The current instance to supported fluent-style method-chaining.</returns>
        public TSelf Assembly<T>()
        {
            Assemblies(typeof(T));
            return (TSelf)this;
        }

        /// <summary>
        /// Adds the <typeparamref name="T1"/> and <typeparamref name="T2"/> <see cref="System.Reflection.Assembly"/> containing the embedded resources (shortcut to the <see cref="MigrationConsoleBase.Args"/> <see cref="MigrationArgsBase.Assemblies"/>.
        /// </summary>
        /// <typeparam name="T1">The <see cref="Type"/> to infer <see cref="System.Reflection.Assembly"/>.</typeparam>
        /// <typeparam name="T2">The <see cref="Type"/> to infer <see cref="System.Reflection.Assembly"/>.</typeparam>
        /// <returns>The current instance to supported fluent-style method-chaining.</returns>
        public TSelf Assembly<T1, T2>()
        {
            Assemblies(typeof(T1), typeof(T2));
            return (TSelf)this;
        }

        /// <summary>
        /// Adds the <typeparamref name="T1"/>, <typeparamref name="T2"/> and <typeparamref name="T3"/> <see cref="System.Reflection.Assembly"/> containing the embedded resources (shortcut to the <see cref="MigrationConsoleBase.Args"/> <see cref="MigrationArgsBase.Assemblies"/>.
        /// </summary>
        /// <typeparam name="T1">The <see cref="Type"/> to infer <see cref="System.Reflection.Assembly"/>.</typeparam>
        /// <typeparam name="T2">The <see cref="Type"/> to infer <see cref="System.Reflection.Assembly"/>.</typeparam>
        /// <typeparam name="T3">The <see cref="Type"/> to infer <see cref="System.Reflection.Assembly"/>.</typeparam>
        /// <returns>The current instance to supported fluent-style method-chaining.</returns>
        public TSelf Assembly<T1, T2, T3>()
        {
            Assemblies(typeof(T1), typeof(T2), typeof(T3));
            return (TSelf)this;
        }

        /// <summary>
        /// Sets (overrides) the output <see cref="DirectoryInfo"/> where the generated artefacts are to be written.
        /// </summary>
        /// <param name="path">The output <see cref="DirectoryInfo"/>.</param>
        /// <returns>The current instance to supported fluent-style method-chaining.</returns>
        public TSelf OutputDirectory(string path)
        {
            Args.OutputDirectory = new DirectoryInfo(path ?? throw new ArgumentNullException(nameof(path)));
            return (TSelf)this;
        }

        /// <summary>
        /// Sets (overrides) the <see cref="MigrationConsoleBase.SupportedCommands"/>.
        /// </summary>
        /// <param name="supportedCommands">The supported <see cref="MigrationCommand"/>(s)</param>
        /// <returns>The current instance to supported fluent-style method-chaining.</returns>
        public TSelf Supports(MigrationCommand supportedCommands)
        {
            SupportedCommands = supportedCommands;
            return (TSelf)this;
        }

        /// <summary>
        /// Indicates whether to automatically accept any confirmation prompts (command-line execution only).
        /// </summary>
        /// <returns>The current instance to supported fluent-style method-chaining.</returns>
        public TSelf AcceptsPrompts()
        {
            Args.AcceptPrompts = true;
            return (TSelf)this;
        }
    }
}