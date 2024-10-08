﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;
using DbEx.Migration;

namespace DbEx
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
        Migrate = 4,

        /// <summary>
        /// Generates the likes of database <b>Schema</b> objects via code-generation (where applicable).
        /// </summary>
        /// <remarks>The <see cref="DatabaseMigrationBase.IsCodeGenEnabled"/> must be set <c>true</c> to enable execution within the orchestration flow.</remarks>
        CodeGen = 8,

        /// <summary>
        /// Drops and creates the known database <b>Schema</b> objects.
        /// </summary>
        /// <remarks>These are generally schema related artefacts that are applied as scripted on every invocation. These may be deleted (where underlying object is pre-existing) and then (re-)applied where object type is known.</remarks>
        Schema = 16,

        /// <summary>
        /// Resets the database by deleting all existing data.  
        /// </summary>
        /// <remarks>This is intended for development and testing purposes only; therefore, this should never be used in a production environment.</remarks>
        Reset = 32,

        /// <summary>
        /// Inserts or merges <b>Data</b> from embedded YAML files.
        /// </summary>
        Data = 64,

        /// <summary>
        /// Performs <b>all</b> the primary commands as follows; <see cref="Create"/>, <see cref="Migrate"/>, <see cref="CodeGen"/>, <see cref="Schema"/> and <see cref="Data"/>.
        /// </summary>
        All = Create | Migrate | CodeGen | Schema | Data,

        /// <summary>
        /// Performs <see cref="Create"/>, <see cref="Migrate"/> and <see cref="CodeGen"/>.
        /// </summary>
        /// <remarks>This can be useful in development scenarios where the <see cref="CodeGen"/> results in a new migration script that needs to be applied before any corresponding <see cref="Schema"/> operations are performed.</remarks>
        CreateMigrateAndCodeGen = Create | Migrate | CodeGen,

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
        /// Performs only the <b>database</b> commands as follows: <see cref="Create"/>, <see cref="Migrate"/>, <see cref="Schema"/> and <see cref="Data"/>.
        /// </summary>
        Database = Create | Migrate | Schema | Data,

        /// <summary>
        /// Performs <see cref="Drop"/> and <see cref="Database"/>.
        /// </summary>
        DropAndDatabase = Drop | Database,

        /// <summary>
        /// Performs <see cref="Reset"/> and <see cref="Database"/>.
        /// </summary>
        ResetAndDatabase = Reset | Database,

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