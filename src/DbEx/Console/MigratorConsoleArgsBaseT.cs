﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;
using System.Collections.Generic;
using System.Reflection;

namespace DbEx.Console
{
    /// <summary>
    /// Provides the base <see cref="MigratorConsoleBase"/> arguments.
    /// </summary>
    /// <typeparam name="TSelf">The <see cref="Type"/> itself being implemented.</typeparam>
    public abstract class MigratorConsoleArgsBase<TSelf> : MigratorConsoleArgsBase where TSelf : MigratorConsoleArgsBase<TSelf>
    {
        /// <summary>
        /// Adds (inserts) one or more <paramref name="assemblies"/> to <see cref="MigratorConsoleArgsBase.Assemblies"/> (before any existing values).
        /// </summary>
        /// <param name="assemblies">The assemblies to add.</param>
        /// <remarks>The order in which they are specified is the order in which they will be probed for embedded resources.</remarks>
        /// <returns>The current <see cref="MigratorConsoleArgsBase{TSelf}"/> instance to support fluent-style method-chaining.</returns>
        public new TSelf AddAssembly(params Assembly[] assemblies)
        {
            base.AddAssembly(assemblies);
            return (TSelf)this;
        }

        /// <summary>
        /// Adds (inserts) one or more <paramref name="types"/> (being their underlying <see cref="System.Type.Assembly"/>) to <see cref="MigratorConsoleArgsBase.Assemblies"/> (before any existing values).
        /// </summary>
        /// <param name="types">The types to add.</param>
        /// <remarks>The order in which they are specified is the order in which they will be probed for embedded resources.</remarks>
        /// <returns>The current <see cref="MigratorConsoleArgs"/> instance to support fluent-style method-chaining.</returns>
        public TSelf AddAssembly(params System.Type[] types)
        {
            var list = new List<Assembly>();
            foreach (var t in types)
            {
                list.Add(t.Assembly);
            }

            return AddAssembly(list.ToArray());
        }

        /// <summary>
        /// Adds one or more <paramref name="schemas"/> to the <see cref="MigratorConsoleArgsBase.SchemaOrder"/>.
        /// </summary>
        /// <param name="schemas">The schemas to add.</param>
        /// <returns>The current <see cref="MigratorConsoleArgsBase{TSelf}"/> instance to support fluent-style method-chaining.</returns>
        public TSelf AddSchemaOrder(params string[] schemas)
        {
            SchemaOrder.AddRange(schemas);
            return (TSelf)this;
        }
    }
}