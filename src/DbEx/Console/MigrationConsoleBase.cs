// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx;
using DbEx.Migration;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using OnRamp;
using OnRamp.Console;
using OnRamp.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DbEx.Console
{
    /// <summary>
    /// Base console that facilitates the <see cref="DatabaseMigrationBase"/> by managing the standard console command-line arguments/options.
    /// </summary>
    /// <remarks>The standard console command-line arguments/options can be controlled via the constructor using the <see cref="SupportedOptions"/> flags. Additional capabilities can be added by inheriting and overriding the
    /// <see cref="OnBeforeExecute(CommandLineApplication)"/>, <see cref="OnValidation(ValidationContext)"/> and <see cref="OnMigrateAsync"/>. Changes to the console output can be achieved by overriding
    /// <see cref="OnWriteMasthead"/>, <see cref="OnWriteHeader"/>, <see cref="OnWriteArgs(DatabaseMigrationBase)"/> and <see cref="OnWriteFooter(double)"/>.
    /// <para>The underlying command line parsing is provided by <see href="https://natemcmaster.github.io/CommandLineUtils/"/>.</para></remarks>
    public abstract class MigrationConsoleBase
    {
        private static readonly string[] memberNames = ["args"];
        private const string EntryAssemblyOnlyOptionName = "entry-assembly-only";
        private const string AcceptPromptsOptionName = "accept-prompts";
        private const string DropSchemaObjectsName = "drop-schema-objects";
        private CommandArgument<MigrationCommand>? _commandArg;
        private CommandArgument? _additionalArgs;
        private CommandOption? _helpOption;

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationConsoleBase"/> class.
        /// </summary>
        /// <param name="args">The default <see cref="MigrationArgsBase"/> that will be overridden/updated by the command-line argument values.</param>
        protected MigrationConsoleBase(MigrationArgsBase args) => Args = args.ThrowIfNull(nameof(args));

        /// <summary>
        /// Gets the <see cref="MigrationArgsBase"/>.
        /// </summary>
        public MigrationArgsBase Args { get; }

        /// <summary>
        /// Gets the application/command name.
        /// </summary>
        public virtual string AppName => (Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()).GetName()?.Name ?? "UNKNOWN";

        /// <summary>
        /// Gets the application/command title. 
        /// </summary>
        public virtual string AppTitle => $"{AppName} Database Tool.";

        /// <summary>
        /// Gets the <see cref="Args"/> <see cref="MigrationArgsBase.Logger"/>.
        /// </summary>
        protected ILogger? Logger => Args.Logger;

        /// <summary>
        /// Gets the console (command line) options.
        /// </summary>
        protected Dictionary<string, CommandOption?> ConsoleOptions { get; } = [];

        /// <summary>
        /// Indicates whether to bypass standard execution of <see cref="OnWriteMasthead"/>, <see cref="OnWriteHeader"/>, <see cref="OnWriteArgs(DatabaseMigrationBase)"/> and <see cref="OnWriteFooter(double)"/>.
        /// </summary>
        protected bool BypassOnWrites { get; set; }

        /// <summary>
        /// Gets or sets the masthead text used by <see cref="OnWriteMasthead"/>.
        /// </summary>
        /// <remarks>Defaults to 'OnRamp Code-Gen Tool' formatted using <see href="https://www.patorjk.com/software/taag/#p=display&amp;f=Calvin%20S&amp;t=DbEx%20Database%20Tool"/>.</remarks>
        public string? MastheadText { get; protected set; } = @"
╔╦╗┌┐ ╔═╗─┐ ┬  ╔╦╗┌─┐┌┬┐┌─┐┌┐ ┌─┐┌─┐┌─┐  ╔╦╗┌─┐┌─┐┬  
 ║║├┴┐║╣ ┌┴┬┘   ║║├─┤ │ ├─┤├┴┐├─┤└─┐├┤    ║ │ ││ ││  
═╩╝└─┘╚═╝┴ └─  ═╩╝┴ ┴ ┴ ┴ ┴└─┘┴ ┴└─┘└─┘   ╩ └─┘└─┘┴─┘
";

        /// <summary>
        /// Gets or sets the supported <see cref="MigrationCommand"/>(s); where executed with an unsupported command an error will occur.
        /// </summary>
        /// <remarks>Defaults to everything: <see cref="MigrationCommand.All"/>, <see cref="MigrationCommand.Reset"/>, <see cref="MigrationCommand.Drop"/>, <see cref="MigrationCommand.Execute"/> and <see cref="MigrationCommand.Script"/>.</remarks>
        public MigrationCommand SupportedCommands { get; set; } = MigrationCommand.All | MigrationCommand.Reset | MigrationCommand.Drop | MigrationCommand.Execute | MigrationCommand.Script;

        /// <summary>
        /// Runs the code generation using the passed <paramref name="migrationCommand"/>.
        /// </summary>
        /// <param name="migrationCommand">The <see cref="MigrationCommand"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><b>Zero</b> indicates success; otherwise, unsuccessful.</returns>
        public async Task<int> RunAsync(MigrationCommand migrationCommand, CancellationToken cancellationToken = default) => await RunAsync(migrationCommand.ToString(), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Runs the code generation using the passed <paramref name="args"/> string.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><b>Zero</b> indicates success; otherwise, unsuccessful.</returns>
        public async Task<int> RunAsync(string? args = null, CancellationToken cancellationToken = default) => await RunAsync(CodeGenConsole.SplitArgumentsIntoArray(args), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Runs the code generation using the passed <paramref name="args"/> array.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><b>Zero</b> indicates success; otherwise, unsuccessful.</returns>
        public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
        {
            Args.Logger ??= new ConsoleLogger(PhysicalConsole.Singleton);
            HandlebarsHelpers.Logger ??= Args.Logger;

            // Set up the app.
            using var app = new CommandLineApplication(PhysicalConsole.Singleton) { Name = AppName, Description = AppTitle };
            _helpOption = app.HelpOption();

            _commandArg = app.Argument<MigrationCommand>("command", "Database migration command (see https://github.com/Avanade/dbex#commands-functions).").IsRequired();
            ConsoleOptions.Add(nameof(OnRamp.CodeGeneratorDbArgsBase.ConnectionString), app.Option("-cs|--connection-string", "Database connection string.", CommandOptionType.SingleValue));
            ConsoleOptions.Add(nameof(OnRamp.CodeGeneratorDbArgsBase.ConnectionStringEnvironmentVariableName), app.Option("-cv|--connection-varname", "Database connection string environment variable name.", CommandOptionType.SingleValue));
            ConsoleOptions.Add(nameof(MigrationArgsBase.SchemaOrder), app.Option("-so|--schema-order", "Database schema name (multiple can be specified in priority order).", CommandOptionType.MultipleValue));
            ConsoleOptions.Add(nameof(MigrationArgsBase.OutputDirectory), app.Option("-o|--output", "Output directory path.", CommandOptionType.MultipleValue).Accepts(v => v.ExistingDirectory("Output directory path does not exist.")));
            ConsoleOptions.Add(nameof(MigrationArgsBase.Assemblies), app.Option("-a|--assembly", "Assembly containing embedded resources (multiple can be specified in probing order).", CommandOptionType.MultipleValue));
            ConsoleOptions.Add(nameof(MigrationArgsBase.Parameters), app.Option("-p|--param", "Parameter expressed as a 'Name=Value' pair (multiple can be specified).", CommandOptionType.MultipleValue));
            ConsoleOptions.Add(EntryAssemblyOnlyOptionName, app.Option("-eo|--entry-assembly-only", "Use the entry assembly only (ignore all other assemblies).", CommandOptionType.NoValue));
            ConsoleOptions.Add(DropSchemaObjectsName, app.Option("-dso|--drop-schema-objects", "Drop all known schema objects before applying; bypasses automatic skip where all scripts are replacements.", CommandOptionType.NoValue));
            ConsoleOptions.Add(AcceptPromptsOptionName, app.Option("--accept-prompts", "Accept prompts; command should _not_ stop and wait for user confirmation (DROP or RESET commands).", CommandOptionType.NoValue));
            _additionalArgs = app.Argument("args", "Additional arguments; 'Script' arguments (first being the script name) -or- 'Execute' (each a SQL statement to invoke).", multipleValues: true);

            OnBeforeExecute(app);

            // Set up the validation.
            app.OnValidate(ctx =>
            {
                Args.MigrationCommand = _commandArg.ParsedValue;
                if (!SupportedCommands.HasFlag(Args.MigrationCommand))
                    return new ValidationResult($"The specified database migration command is not supported.");

                // Update the options from command line.
                var so = GetCommandOption(nameof(MigrationArgsBase.SchemaOrder));
                if (so.HasValue())
                {
                    Args.SchemaOrder.Clear();
                    Args.SchemaOrder.AddRange(so.Values.Where(x => !string.IsNullOrEmpty(x)).OfType<string>().Distinct());
                }

                UpdateStringOption(nameof(MigrationArgsBase.OutputDirectory), v => Args.OutputDirectory = new DirectoryInfo(v));

                var vr = ValidateMultipleValue(nameof(MigrationArgsBase.Assemblies), ctx, (ctx, co) => new AssemblyValidator(Args).GetValidationResult(co, ctx));
                if (vr != ValidationResult.Success)
                    return vr;

                UpdateBooleanOption(EntryAssemblyOnlyOptionName, () =>
                {
                    Args.ClearAssemblies();
                    Args.AddAssembly(Assembly.GetEntryAssembly()!);
                });

                vr = ValidateMultipleValue(nameof(MigrationArgsBase.Parameters), ctx, (ctx, co) => new ParametersValidator(Args).GetValidationResult(co, ctx));
                if (vr != ValidationResult.Success)
                    return vr;

                if (_additionalArgs.Values.Count > 0 && !(Args.MigrationCommand.HasFlag(MigrationCommand.CodeGen) || Args.MigrationCommand.HasFlag(MigrationCommand.Script) || Args.MigrationCommand.HasFlag(MigrationCommand.Execute)))
                    return new ValidationResult($"Additional arguments can only be specified when the command is '{nameof(MigrationCommand.CodeGen)}', '{nameof(MigrationCommand.Script)}' or '{nameof(MigrationCommand.Execute)}'.", memberNames);

                if (Args.MigrationCommand.HasFlag(MigrationCommand.CodeGen) || Args.MigrationCommand.HasFlag(MigrationCommand.Script))
                {
                    for (int i = 0; i < _additionalArgs.Values.Count; i++)
                    {
                        Args.Parameters.Add($"Param{i}", _additionalArgs.Values[i]);
                    }
                }

                if (Args.MigrationCommand.HasFlag(MigrationCommand.Execute))
                {
                    for (int i = 0; i < _additionalArgs.Values.Count; i++)
                    {
                        if (string.IsNullOrEmpty(_additionalArgs.Values[i]))
                            continue;

                        Args.ExecuteStatements ??= [];
                        Args.ExecuteStatements.Add(_additionalArgs.Values[i]!);
                    }
                }

                // Handle the connection string, in order of precedence: command-line argument, environment variable, what was passed as initial argument.
                var cs = GetCommandOption(nameof(OnRamp.CodeGeneratorDbArgsBase.ConnectionString));
                var evn = GetCommandOption(nameof(OnRamp.CodeGeneratorDbArgsBase.ConnectionStringEnvironmentVariableName))?.Value();
                if (!string.IsNullOrEmpty(evn))
                    Args.ConnectionStringEnvironmentVariableName = evn;

                Args.OverrideConnectionString(cs?.Value());

                // Invoke any additional.
                var res = OnValidation(ctx)!;

                // Action any command input.
                var nco = GetCommandOption(AcceptPromptsOptionName);
                if (nco == null || !nco.HasValue())
                {
                    if (Args.MigrationCommand.HasFlag(MigrationCommand.Drop))
                    {
                        if (!Args.AcceptPrompts && !Prompt.GetYesNo("DROP: Confirm that where the specified database already exists it should be dropped?", false, ConsoleColor.Yellow))
                            return new ValidationResult("Database drop was not confirmed; no execution occurred.");
                    }
                    else if (Args.MigrationCommand.HasFlag(MigrationCommand.Reset))
                    {
                        if (!Args.AcceptPrompts && !Prompt.GetYesNo("RESET: Confirm that the existing data within the database should be reset (deleted)?", false, ConsoleColor.Yellow))
                            return new ValidationResult("Data reset was not confirmed; no execution occurred.");
                    }
                }

                // Handle drop schema objects.
                var dso = GetCommandOption(DropSchemaObjectsName);
                if (dso is not null && dso.HasValue())
                    Args.DropSchemaObjects = true;

                return res;
            });

            // Set up the code generation execution.
            app.OnExecuteAsync(RunRunawayAsync);

            // Execute the command-line app.
            try
            {
                var result = await app.ExecuteAsync(args, cancellationToken).ConfigureAwait(false);

                if (result == 0 && _helpOption.HasValue())
                    OnWriteHelp();

                return result;
            }
            catch (CommandParsingException cpex)
            {
                Args.Logger?.LogError("{Content}", cpex.Message);
                Args.Logger?.LogError("{Content}", string.Empty);
                return 1;
            }
        }

        /// <summary>
        /// Gets the selected <see cref="CommandOption"/> for the specfied <paramref name="option"/>.
        /// </summary>
        /// <param name="option">The option name.</param>
        /// <returns>The corresponding <see cref="CommandOption"/>.</returns>
        protected CommandOption GetCommandOption(string option) => ConsoleOptions.GetValueOrDefault(option) ?? throw new InvalidOperationException($"Command option '{option}' does not exist.");

        /// <summary>
        /// Updates the command option from a string option.
        /// </summary>
        /// <param name="option">The option name.</param>
        /// <param name="action">The action to perform where <paramref name="option"/> is provided.</param>
        protected void UpdateStringOption(string option, Action<string> action)
        {
            var co = GetCommandOption(option);
            if (co != null && co.HasValue())
            {
                var val = co.Value();
                if (!string.IsNullOrEmpty(val))
                    action.Invoke(val);
            }
        }

        /// <summary>
        /// Updates the command option from a boolean option.
        /// </summary>
        /// <param name="option">The option name.</param>
        /// <param name="action">The action to perform where <paramref name="option"/> is provided.</param>
        protected void UpdateBooleanOption(string option, Action action)
        {
            var co = GetCommandOption(option);
            if (co != null && co.HasValue())
                action.Invoke();
        }

        /// <summary>
        /// Validate multiple options.
        /// </summary>
        /// <param name="option">The option name.</param>
        /// <param name="ctx">The <see cref="ValidationContext"/>.</param>
        /// <param name="func">The function to perform where <paramref name="option"/> is provided.</param>
        protected ValidationResult ValidateMultipleValue(string option, ValidationContext ctx, Func<ValidationContext, CommandOption, ValidationResult> func)
        {
            var co = GetCommandOption(option);
            if (co == null)
                return ValidationResult.Success!;
            else
                return func(ctx, co);
        }

        /// <summary>
        /// Invoked before the underlying console execution occurs.
        /// </summary>
        /// <param name="app">The underlying <see cref="CommandLineApplication"/>.</param>
        /// <remarks>This enables additional configuration to the <paramref name="app"/> prior to execution. For example, adding additional command line arguments.</remarks>
        protected virtual void OnBeforeExecute(CommandLineApplication app) { }

        /// <summary>
        /// Invoked after command parsing is complete and before the underlying code-generation.
        /// </summary>
        /// <param name="context">The <see cref="ValidationContext"/>.</param>
        /// <returns>The <see cref="ValidationResult"/>.</returns>
        protected virtual ValidationResult? OnValidation(ValidationContext context) => ValidationResult.Success;

        /// <summary>
        /// Performs the actual code-generation.
        /// </summary>
        private async Task<int> RunRunawayAsync(CancellationToken cancellationToken) /* Method name inspired by: Slade - Run Runaway - https://www.youtube.com/watch?v=gMxcGaAwy-Q */
        {
            try
            {
                // Create the migrator.
                using var migrator = CreateMigrator();

                // Write header, etc.
                if (!BypassOnWrites)
                {
                    OnWriteMasthead();
                    OnWriteHeader();
                    OnWriteArgs(migrator);
                }

                // Run the code generator.
                var sw = Stopwatch.StartNew();
                if (!await OnMigrateAsync(migrator, cancellationToken).ConfigureAwait(false))
                    return 3;

                // Write footer and exit successfully.
                sw.Stop();
                if (!BypassOnWrites)
                    OnWriteFooter(sw.Elapsed.TotalMilliseconds);

                return 0;
            }
            catch (CodeGenException gcex)
            {
                if (gcex.Message != null)
                {
                    Args.Logger?.LogError("{Content}", gcex.Message);
                    if (gcex.InnerException != null)
                        Args.Logger?.LogError("{Content}", gcex.InnerException.Message);

                    Args.Logger?.LogError("{Content}", string.Empty);
                }

                return 2;
            }
        }

        /// <summary>
        /// Invoked to execute the <see cref="DatabaseMigrationBase"/>.
        /// </summary>
        /// <param name="migrator">The <see cref="DatabaseMigrationBase"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        protected virtual async Task<bool> OnMigrateAsync(DatabaseMigrationBase migrator, CancellationToken cancellationToken)
        {
            // Perform migration.
            if (!await migrator.MigrateAsync(cancellationToken).ConfigureAwait(false))
                return false;

            Logger?.LogInformation("{Content}", string.Empty);
            Logger?.LogInformation("{Content}", new string('-', 80));

            return true;
        }

        /// <summary>
        /// Creates the <see cref="DatabaseMigrationBase"/> that is used to perform the database migration orchestration.
        /// </summary>
        /// <returns>The <see cref="DatabaseMigrationBase"/>.</returns>
        protected abstract DatabaseMigrationBase CreateMigrator();

        /// <summary>
        /// Invoked to write the <see cref="MastheadText"/> to the <see cref="Logger"/>.
        /// </summary>
        protected virtual void OnWriteMasthead()
        {
            if (MastheadText != null)
                Logger?.LogInformation("{Content}", MastheadText);
        }

        /// <summary>
        /// Invoked to write the header information to the <see cref="Logger"/>.
        /// </summary>
        /// <remarks>Writes the <see cref="AppTitle"/>.</remarks>
        protected virtual void OnWriteHeader()
        {
            Logger?.LogInformation("{Content}", AppTitle);
            Logger?.LogInformation("{Content}", string.Empty);
        }

        /// <summary>
        /// Invoked to write the <see cref="Args"/> to the <see cref="Logger"/>.
        /// </summary>
        /// <param name="migrator">The <see cref="DatabaseMigrationBase"/> to write.</param>
        protected virtual void OnWriteArgs(DatabaseMigrationBase migrator) => WriteStandardizedArgs(migrator);

        /// <summary>
        /// Write the <paramref name="migrator"/> context to the <see cref="Logger"/> in a standardized (reusable) manner.
        /// </summary>
        /// <param name="migrator">The <see cref="DatabaseMigrationBase"/> to write.</param>
        /// <param name="additional">Provides an optional opportunity to log/write additional information.</param>
        public static void WriteStandardizedArgs(DatabaseMigrationBase migrator, Action<ILogger>? additional = null)
        {
            if (migrator.Args.Logger == null)
                return;

            migrator.Args.Logger.LogInformation("{Content}", $"Command = {migrator.Args.MigrationCommand}");
            migrator.Args.Logger.LogInformation("{Content}", $"Provider = {migrator.Provider}");
            migrator.Args.Logger.LogInformation("{Content}", $"SchemaOrder = {string.Join(", ", [.. migrator.Args.SchemaOrder])}");
            migrator.Args.Logger.LogInformation("{Content}", $"OutDir = {migrator.Args.OutputDirectory?.FullName}");

            additional?.Invoke(migrator.Args.Logger);

            migrator.Args.Logger.LogInformation("{Content}", $"Parameters{(migrator.Args.Parameters.Count == 0 ? " = none" : ":")}");
            foreach (var p in migrator.Args.Parameters.OrderBy(x => x.Key))
            {
                migrator.Args.Logger.LogInformation("{Content}", $"  {p.Key} = {p.Value}");
            }

            migrator.Args.Logger.LogInformation("{Content}", $"Assemblies{(!migrator.Args.Assemblies.Any() ? " = none" : ":")}");
            foreach (var a in migrator.Args.Assemblies)
            {
                migrator.Args.Logger.LogInformation("{Content}", $"  {a.Assembly.FullName}");
            }
        }

        /// <summary>
        /// Invoked to write the footer information to the <see cref="Logger"/>.
        /// </summary>
        /// <param name="totalMilliseconds">The elapsed execution time in milliseconds.</param>
        protected virtual void OnWriteFooter(double totalMilliseconds)
        {
            Logger?.LogInformation("{Content}", string.Empty);
            Logger?.LogInformation("{Content}", $"{AppName} Complete. [{totalMilliseconds}ms]");
            Logger?.LogInformation("{Content}", string.Empty);
        }

        /// <summary>
        /// Invoked to write additional help information to the <see cref="Logger"/>.
        /// </summary>
        protected virtual void OnWriteHelp() { }
    }
}