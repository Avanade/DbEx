// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.Console;
using DbEx.Migration;
using DbEx.SqlServer.Migration;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;

namespace DbEx.SqlServer.Console
{
    /// <summary>
    /// Console that facilitates the <see cref="SqlServerMigration"/> by managing the standard console command-line arguments/options.
    /// </summary>
    public sealed class SqlServerMigrationConsole : MigrationConsoleBase<SqlServerMigrationConsole>
    {
        /// <summary>
        /// Creates a new <see cref="SqlServerMigrationConsole"/> using <typeparamref name="T"/> to default the probing <see cref="Assembly"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/>.</typeparam>
        /// <param name="connectionString">The database connection string.</param>
        /// <returns>A new <see cref="SqlServerMigrationConsole"/>.</returns>
        public static SqlServerMigrationConsole Create<T>(string connectionString) => new(new MigrationArgs { ConnectionString = connectionString }.AddAssembly(typeof(T).Assembly));

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerMigrationConsole"/> class.
        /// </summary>
        /// <param name="args">The default <see cref="MigrationArgs"/> that will be overridden/updated by the command-line argument values.</param>
        public SqlServerMigrationConsole(MigrationArgs? args = null) : base(args ?? new MigrationArgs()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerMigrationConsole"/> class that provides a default for the <paramref name="connectionString"/>.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        public SqlServerMigrationConsole(string connectionString) : base(new MigrationArgs { ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString)) }) { }

        /// <summary>
        /// Gets the <see cref="MigrationArgs"/>.
        /// </summary>
        public new MigrationArgs Args => (MigrationArgs)base.Args;

        /// <inheritdoc/>
        protected override DatabaseMigrationBase CreateMigrator() => new SqlServerMigration(Args);

        /// <inheritdoc/>
        public override string AppTitle => base.AppTitle + " [SQL Server]";

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
            Logger?.LogInformation("{help}", "  script [default]                  Creates a default (empty) SQL script.");
            Logger?.LogInformation("{help}", "  script alter <Schema> <Table>     Creates a SQL script to perform an ALTER TABLE.");
            Logger?.LogInformation("{help}", "  script cdc <Schema> <Table>       Creates a SQL script to turn on CDC for the specified table.");
            Logger?.LogInformation("{help}", "  script cdcdb                      Creates a SQL script to turn on CDC for the database.");
            Logger?.LogInformation("{help}", "  script create <Schema> <Table>    Creates a SQL script to perform a CREATE TABLE.");
            Logger?.LogInformation("{help}", "  script refdata <Schema> <Table>   Creates a SQL script to perform a CREATE TABLE as reference data.");
            Logger?.LogInformation("{help}", "  script schema <Schema>            Creates a SQL script to perform a CREATE SCHEMA.");
        }
    }
}