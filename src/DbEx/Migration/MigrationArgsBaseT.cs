﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;
using System.Collections.Generic;
using System.Reflection;

namespace DbEx.Migration
{
    /// <summary>
    /// Provides the base <see cref="DatabaseMigrationBase"/> arguments.
    /// </summary>
    /// <typeparam name="TSelf">The <see cref="Type"/> itself being implemented.</typeparam>
    public abstract class MigrationArgsBase<TSelf> : MigrationArgsBase where TSelf : MigrationArgsBase<TSelf>
    {
        /// <summary>
        /// Adds (inserts) one or more <paramref name="assemblies"/> to <see cref="MigrationArgsBase.Assemblies"/> (before any existing values; i.e. last in first out/probed).
        /// </summary>
        /// <param name="assemblies">The assemblies to add.</param>
        /// <remarks>The order in which they are specified is the order in which they will be probed for embedded resources.</remarks>
        /// <returns>The current <see cref="MigrationArgsBase{TSelf}"/> instance to support fluent-style method-chaining.</returns>
        public new TSelf AddAssembly(params Assembly[] assemblies)
        {
            base.AddAssembly(assemblies);
            return (TSelf)this;
        }

        /// <summary>
        /// Adds (inserts) one or more <paramref name="types"/> (being the underlying <see cref="Type.Assembly"/>) to <see cref="MigrationArgsBase.Assemblies"/> (before any existing values; i.e. last in first out/probed).
        /// </summary>
        /// <param name="types">The types to add.</param>
        /// <remarks>The order in which they are specified is the order in which they will be probed for embedded resources.</remarks>
        /// <returns>The current <see cref="MigrationArgs"/> instance to support fluent-style method-chaining.</returns>
        public TSelf AddAssembly(params Type[] types)
        {
            var list = new List<Assembly>();
            foreach (var t in types)
            {
                list.Add(t.Assembly);
            }

            return AddAssembly(list.ToArray());
        }

        /// <summary>
        /// Adds (inserts) the <typeparamref name="TAssembly"/> (being the underlying <see cref="Type.Assembly"/>) to <see cref="MigrationArgsBase.Assemblies"/> (before any existing values; i.e. last in first out/probed).
        /// </summary>
        public TSelf AddAssembly<TAssembly>() => AddAssembly(typeof(TAssembly));

        /// <summary>
        /// Adds one or more <paramref name="schemas"/> to the <see cref="MigrationArgsBase.SchemaOrder"/>.
        /// </summary>
        /// <param name="schemas">The schemas to add.</param>
        /// <returns>The current <see cref="MigrationArgsBase{TSelf}"/> instance to support fluent-style method-chaining.</returns>
        public TSelf AddSchemaOrder(params string[] schemas)
        {
            SchemaOrder.AddRange(schemas);
            return (TSelf)this;
        }
    }
}