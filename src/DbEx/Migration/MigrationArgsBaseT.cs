// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

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
        /// Adds one or more <paramref name="assemblies"/> to the <see cref="MigrationArgsBase.Assemblies"/>.
        /// </summary>
        /// <param name="assemblies">The assemblies to add.</param>
        /// <returns>The current <see cref="MigrationArgsBase{TSelf}"/> instance to support fluent-style method-chaining.</returns>
        public new TSelf AddAssembly(params Assembly[] assemblies)
        {
            base.AddAssembly(assemblies);
            return (TSelf)this;
        }

        /// <summary>
        /// Adds one or more <paramref name="assemblies"/> to the <see cref="MigrationArgsBase.Assemblies"/>.
        /// </summary>
        /// <param name="assemblies">The assemblies to add.</param>
        /// <returns>The current <see cref="MigrationArgsBase{TSelf}"/> instance to support fluent-style method-chaining.</returns>
        public new TSelf AddAssembly(params MigrationAssemblyArgs[] assemblies)
        {
            base.AddAssembly(assemblies);
            return (TSelf)this;
        }

        /// <summary>
        /// Adds one or more <paramref name="types"/> (being the underlying <see cref="Type.Assembly"/>) to <see cref="MigrationArgsBase.Assemblies"/>.
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
        /// Adds the <typeparamref name="TAssembly"/> (being the underlying <see cref="Type.Assembly"/>) to <see cref="MigrationArgsBase.Assemblies"/>.
        /// </summary>
        /// <param name="dataNamespaces">The <see cref="MigrationAssemblyArgs.DataNamespaces"/>; defaults to <see cref="MigrationAssemblyArgs.DefaultDataNamespace"/>.</param>
        public TSelf AddAssembly<TAssembly>(params string[] dataNamespaces) => AddAssembly(new MigrationAssemblyArgs(typeof(TAssembly).Assembly, dataNamespaces));

        /// <summary>
        /// Adds a parameter to the <see cref="MigrationArgsBase.Parameters"/> where it does not already exist; unless <paramref name="overrideExisting"/> is selected then it will add or override.
        /// </summary>
        /// <param name="key">The parameter key.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="overrideExisting">Indicates whether to override the existing value where it is pre-existing; otherwise, will not add/update.</param>
        /// <returns>The current <see cref="MigrationArgs"/> instance to support fluent-style method-chaining.</returns>
        public new TSelf AddParameter(string key, object? value, bool overrideExisting = false)
        {
            base.AddParameter(key, value, overrideExisting);
            return (TSelf)this;
        }

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

        /// <summary>
        /// Adds a <see cref="ExplicitMigrationScript"/> being an explicitly named resource-based script to be included (executed) as per the specified <see cref="MigrationCommand"/> (phase).
        /// </summary>
        /// <param name="command">The <see cref="MigrationCommand"/> (phase) where the script should be executed.</param>
        /// <param name="assembly">The <see cref="Assembly"/> where the script resource resides.</param>
        /// <param name="name">The corresponding resource name within the <see cref="Assembly"/>.</param>
        /// <remarks>The <paramref name="command"/> must be a single value; currently only <see cref="MigrationCommand.Migrate"/> and <see cref="MigrationCommand.Schema"/> are supported. This represents the phase in which the script will be 
        /// included for execution.</remarks>
        public new TSelf AddScript(MigrationCommand command, Assembly assembly, string name)
        {
            base.AddScript(command, assembly, name);
            return (TSelf)this;
        }

        /// <summary>
        /// Adds a <see cref="ExplicitMigrationScript"/> being an explicitly named resource-based script to be included (executed) as per the specified <see cref="MigrationCommand"/> (phase).
        /// </summary>
        /// <typeparam name="TAssembly">The <see cref="Type"/> to use to infer the underlying <see cref="Type.Assembly"/> where the script resource resides.</typeparam>
        /// <param name="command">The <see cref="MigrationCommand"/> (phase) where the script should be executed.</param>
        /// <param name="name">The corresponding resource name within the <see cref="Assembly"/>.</param>
        /// <remarks>The <paramref name="command"/> must be a single value; currently only <see cref="MigrationCommand.Migrate"/> and <see cref="MigrationCommand.Schema"/> are supported. This represents the phase in which the script will be 
        /// included for execution.</remarks>
        public TSelf AddScript<TAssembly>(MigrationCommand command, string name) => AddScript(command, typeof(TAssembly).Assembly, name);
    }
}