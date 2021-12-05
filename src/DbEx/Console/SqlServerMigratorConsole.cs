// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.Migration;
using DbEx.Migration.SqlServer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace DbEx.Console
{
    /// <summary>
    /// Console that facilitates the <see cref="SqlServerMigrator"/> by managing the standard console command-line arguments/options.
    /// </summary>
    public sealed class SqlServerMigratorConsole : MigratorConsoleBase<SqlServerMigratorConsole>
    {
        /// <summary>
        /// Creates a new <see cref="SqlServerMigratorConsole"/> using <typeparamref name="T"/> to default the probing <see cref="Assembly"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/>.</typeparam>
        /// <param name="connectionString">The database connection string.</param>
        /// <returns>A new <see cref="SqlServerMigratorConsole"/>.</returns>
        public static SqlServerMigratorConsole Create<T>(string connectionString) => new(new MigratorConsoleArgs { ConnectionString = connectionString }.AddAssembly(typeof(T).Assembly));

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerMigratorConsole"/> class.
        /// </summary>
        /// <param name="args">The default <see cref="MigratorConsoleArgs"/> that will be overridden/updated by the command-line argument values.</param>
        public SqlServerMigratorConsole(MigratorConsoleArgs? args = null) : base(args) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerMigratorConsole"/> class that provides a default for the <paramref name="connectionString"/>.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        public SqlServerMigratorConsole(string connectionString) : base(new MigratorConsoleArgs { ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString)) }) { }

        /// <summary>
        /// Executes the <see cref="SqlServerMigrator"/>.
        /// </summary>
        /// <returns><inheritdoc/></returns>
        protected override async Task<bool> OnMigrateAsync()
        {
            var migrator = new SqlServerMigrator(Args.ConnectionString!, Args.MigrationCommand, Args.Logger ?? NullLogger.Instance, Args.Assemblies.ToArray());

            // Where only creating a new script, then quickly do it and get out of here!
            if (Args.MigrationCommand.HasFlag(MigrationCommand.Script))
            {
                Logger?.LogInformation(string.Empty);
                return await migrator.CreateScriptAsync(Args.ScriptName, Args.ScriptArguments).ConfigureAwait(false);
            }

            // Prepare 
            if (Args.DataParserArgs != null)
                migrator.ParserArgs = Args.DataParserArgs;

            if (!await migrator.MigrateAsync().ConfigureAwait(false))
                return false;

            Logger?.LogInformation(string.Empty);
            Logger?.LogInformation(new string('-', 80));

            return true;
        }
    }
}