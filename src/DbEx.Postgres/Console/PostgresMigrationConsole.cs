// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx;
using DbEx.Console;
using DbEx.Migration;
using DbEx.Postgres.Migration;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;

namespace DbEx.Postgres.Console
{
    /// <summary>
    /// Console that facilitates the <see cref="PostgresMigration"/> by managing the standard console command-line arguments/options.
    /// </summary>
    public sealed class PostgresMigrationConsole : MigrationConsoleBase<PostgresMigrationConsole>
    {
        /// <summary>
        /// Creates a new <see cref="PostgresMigrationConsole"/> using <typeparamref name="T"/> to default the probing <see cref="Assembly"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/>.</typeparam>
        /// <param name="connectionString">The database connection string.</param>
        /// <returns>A new <see cref="PostgresMigrationConsole"/>.</returns>
        public static PostgresMigrationConsole Create<T>(string connectionString) => new(new MigrationArgs { ConnectionString = connectionString }.AddAssembly(typeof(T).Assembly));

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgresMigrationConsole"/> class.
        /// </summary>
        /// <param name="args">The default <see cref="MigrationArgs"/> that will be overridden/updated by the command-line argument values.</param>
        public PostgresMigrationConsole(MigrationArgs? args = null) : base(args ?? new MigrationArgs()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgresMigrationConsole"/> class that provides a default for the <paramref name="connectionString"/>.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        public PostgresMigrationConsole(string connectionString) : base(new MigrationArgs { ConnectionString = connectionString.ThrowIfNull(nameof(connectionString)) }) { }

        /// <summary>
        /// Gets the <see cref="MigrationArgs"/>.
        /// </summary>
        public new MigrationArgs Args => (MigrationArgs)base.Args;

        /// <inheritdoc/>
        protected override DatabaseMigrationBase CreateMigrator() => new PostgresMigration(Args);

        /// <inheritdoc/>
        public override string AppTitle => base.AppTitle + " [PostgreSQL]";

        /// <inheritdoc/>
        protected override void OnWriteHelp()
        {
            base.OnWriteHelp();
            WriteScriptHelp();
            Logger?.LogInformation("{help}", string.Empty);
        }

        /// <summary>
        /// Writes the supported <see cref="MigrationCommand.Script"/> help content.
        /// </summary>
        public void WriteScriptHelp()
        {
            Logger?.LogInformation("{help}", "Script command and argument(s):");
            Logger?.LogInformation("{help}", "  script [default]         Creates a default (empty) SQL script.");
            Logger?.LogInformation("{help}", "  script alter <table>     Creates a SQL script to perform an ALTER TABLE.");
            Logger?.LogInformation("{help}", "  script create <table>    Creates a SQL script to perform a CREATE TABLE.");
            Logger?.LogInformation("{help}", "  script refdata <table>   Creates a SQL script to perform a CREATE TABLE as reference data.");
        }
    }
}