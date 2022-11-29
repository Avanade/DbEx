// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.Console;
using DbEx.Migration;
using DbEx.MySql.Migration;
using System;
using System.Reflection;

namespace DbEx.MySql.Console
{
    /// <summary>
    /// Console that facilitates the <see cref="MySqlMigration"/> by managing the standard console command-line arguments/options.
    /// </summary>
    public sealed class MySqlMigrationConsole : MigrationConsoleBase<MySqlMigrationConsole>
    {
        /// <summary>
        /// Creates a new <see cref="MySqlMigrationConsole"/> using <typeparamref name="T"/> to default the probing <see cref="Assembly"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/>.</typeparam>
        /// <param name="connectionString">The database connection string.</param>
        /// <returns>A new <see cref="MySqlMigrationConsole"/>.</returns>
        public static MySqlMigrationConsole Create<T>(string connectionString) => new(new MigrationArgs { ConnectionString = connectionString }.AddAssembly(typeof(T).Assembly));

        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlMigrationConsole"/> class.
        /// </summary>
        /// <param name="args">The default <see cref="MigrationArgs"/> that will be overridden/updated by the command-line argument values.</param>
        public MySqlMigrationConsole(MigrationArgs? args = null) : base(args ?? new MigrationArgs()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlMigrationConsole"/> class that provides a default for the <paramref name="connectionString"/>.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        public MySqlMigrationConsole(string connectionString) : base(new MigrationArgs { ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString)) }) { }

        /// <summary>
        /// Gets the <see cref="MigrationArgs"/>.
        /// </summary>
        public new MigrationArgs Args => (MigrationArgs)base.Args;

        /// <inheritdoc/>
        protected override DatabaseMigrationBase CreateMigrator() => new MySqlMigration(Args);
    }
}