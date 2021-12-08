// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;

namespace DbEx.Migration
{
    /// <summary>
    /// Represents the migration command, in that it controls the underlying migration tasks that are to be performed.
    /// </summary>
    [Flags]
    public enum MigrationCommand
    {
        /// <summary>
        /// Nothing specified.
        /// </summary>
        None = 0,

        /// <summary>
        /// Drop the existing database (where it already exists).
        /// </summary>
        Drop = 1,

        /// <summary>
        /// Create the database (where it does not already exist).
        /// </summary>
        Create = 2,

        /// <summary>
        /// Migrate the database using the <b>Migrations</b> scripts (those that have not already been executed).
        /// </summary>
        /// <remarks>Internally this uses <see href="http://dbup.github.io/"/> to orchestrate schema versions.</remarks>
        Migrate = 4,

        /// <summary>
        /// Drops and creates the known database <b>Schema</b> objects.
        /// </summary>
        /// <remarks>These are generally schema related artefacts that are applied as scripted on every invocation. These may be deleted (where underlying object is pre-existing) and then applied where object type is known.</remarks>
        Schema = 8,

        /// <summary>
        /// Resets the database by deleting all existing data.  
        /// </summary>
        /// <remarks>This is intended for development and testing purposes only; therefore, this should never be used in a production environment.</remarks>
        Reset = 16,

        /// <summary>
        /// Inserts or merges <b>Data</b> from embedded YAML files.
        /// </summary>
        Data = 32,

        /// <summary>
        /// Performs <b>all</b> the primary commands as follows; <see cref="Create"/>, <see cref="Migrate"/>, <see cref="Schema"/> and <see cref="Data"/>.
        /// </summary>
        All = Create | Migrate | Schema | Data,

        /// <summary>
        /// Performs <see cref="Migrate"/> and <see cref="Schema"/>.
        /// </summary>
        Deploy = Migrate | Schema,

        /// <summary>
        /// Performs <see cref="Deploy"/> with <see cref="Data"/>.
        /// </summary>
        DeployWithData = Deploy | Data,

        /// <summary>
        /// Performs <see cref="Drop"/> and <see cref="All"/>.
        /// </summary>
        DropAndAll = Drop | All,

        /// <summary>
        /// Performs <see cref="Reset"/> and <see cref="All"/>.
        /// </summary>
        ResetAndAll = Reset | All,

        /// <summary>
        /// Performs <see cref="Reset"/> and <see cref="Data"/>.
        /// </summary>
        ResetAndData = Reset | Data,

        /// <summary>
        /// Executes the SQL statement(s) passed as additional arguments.
        /// </summary>
        /// <remarks>This can not be used with any of the other commands.</remarks>
        Execute = 1024,

        /// <summary>
        /// Creates a new <see cref="Migrate">migration</see> script file using the defined naming convention.
        /// </summary>
        /// <remarks>This can not be used with any of the other commands.</remarks>
        Script = 2048
    }
}