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
        /// Creates a new <see cref="SqlServerMigratorConsole"/> using <typeparamref name="T"/> to determine <see cref="Assembly"/> and provide a default for the <paramref name="connectionString"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/>.</typeparam>
        /// <param name="connectionString">The database connection string.</param>
        /// <returns>A new <see cref="SqlServerMigratorConsole"/>.</returns>
        public static SqlServerMigratorConsole Create<T>(string connectionString) => new(typeof(T).Assembly, new MigratorConsoleArgs { ConnectionString = connectionString }.AddAssembly(typeof(T).Assembly));

        /// <summary>
        /// Creates a new <see cref="SqlServerMigratorConsole"/> using <typeparamref name="T"/> to determine <see cref="Assembly"/> defaulting <see cref="MigratorConsoleBase.Name"/> (with <see cref="AssemblyName.Name"/>),
        /// <see cref="MigratorConsoleBase.Text"/> (with <see cref="AssemblyProductAttribute.Product"/>), <see cref="MigratorConsoleBase.Description"/> (with <see cref="AssemblyDescriptionAttribute.Description"/>), 
        /// and <see cref="Version"/> (with <see cref="AssemblyName.Version"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/>.</typeparam>
        /// <param name="args">The default <see cref="MigratorConsoleArgs"/> that will be overridden or updated by the command-line argument values.</param>
        /// <param name="name">The application/command name; defaults to <see cref="AssemblyName.Name"/>.</param>
        /// <param name="text">The application/command short text.</param>
        /// <param name="description">The application/command description; defaults to <paramref name="text"/> when not specified.</param>
        /// <param name="version">The application/command version number.</param>
        /// <returns>A new <see cref="SqlServerMigratorConsole"/>.</returns>
        public static SqlServerMigratorConsole Create<T>(MigratorConsoleArgs? args = null, string? name = null, string? text = null, string? description = null, string? version = null)
            => new(typeof(T).Assembly, args, name, text, description, version);

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerMigratorConsole"/> class.
        /// </summary>
        /// <param name="name">The application/command name.</param>
        /// <param name="text">The application/command short text.</param>
        /// <param name="description">The application/command description; will default to <paramref name="text"/> when not specified.</param>
        /// <param name="version">The application/command version number.</param>
        /// <param name="args">The default <see cref="MigratorConsoleArgs"/> that will be overridden/updated by the command-line argument values.</param>
        public SqlServerMigratorConsole(string name, string text, string? description = null, string? version = null, MigratorConsoleArgs? args = null)
            : base(name, text, description, version, args) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerMigratorConsole"/> class defaulting <see cref="MigratorConsoleBase.Name"/> (with <see cref="AssemblyName.Name"/>), <see cref="MigratorConsoleBase.Text"/> 
        /// (with <see cref="AssemblyProductAttribute.Product"/>), <see cref="MigratorConsoleBase.Description"/> (with <see cref="AssemblyDescriptionAttribute.Description"/>), and <see cref="Version"/> 
        /// (with <see cref="AssemblyName.Version"/>) from the <paramref name="assembly"/> where not expressly provided.
        /// </summary>
        /// <param name="args">The default <see cref="MigratorConsoleArgs"/> that will be overridden/updated by the command-line argument values.</param>
        /// <param name="assembly">The <see cref="Assembly"/> to infer properties where not expressly provided.</param>
        /// <param name="name">The application/command name; defaults to <see cref="AssemblyName.Name"/>.</param>
        /// <param name="text">The application/command short text.</param>
        /// <param name="description">The application/command description; defaults to <paramref name="text"/> when not specified.</param>
        /// <param name="version">The application/command version number.</param>
        public SqlServerMigratorConsole(Assembly assembly, MigratorConsoleArgs? args = null, string? name = null, string? text = null, string? description = null, string? version = null)
            : base(assembly, args, name, text, description, version) { }

        /// <summary>
        /// Executes the <see cref="SqlServerMigrator"/>.
        /// </summary>
        /// <returns><inheritdoc/></returns>
        protected override async Task<bool> OnMigrateAsync()
        {
            var migrator = new SqlServerMigrator(Args.ConnectionString!, Args.MigrationCommand, Args.Logger ?? NullLogger.Instance, Args.Assemblies.ToArray());

            // Where only creating a new script, then quickly do it and get out of here!
            if (Args.MigrationCommand.HasFlag(MigrationCommand.Script))
                return await migrator.CreateScriptAsync(Args.ScriptName, Args.ScriptParameters).ConfigureAwait(false);

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