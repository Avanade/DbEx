// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

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
using System.Threading.Tasks;

namespace DbEx.Console
{
    /// <summary>
    /// Base console that facilitates the <see cref="DatabaseMigratorBase"/> by managing the standard console command-line arguments/options.
    /// </summary>
    /// <remarks>The standard console command-line arguments/options can be controlled via the constructor using the <see cref="SupportedOptions"/> flags. Additional capabilities can be added by inherting and overridding the
    /// <see cref="OnBeforeExecute(CommandLineApplication)"/>, <see cref="OnValidation(ValidationContext)"/> and <see cref="OnMigrateAsync"/>. Changes to the console output can be achieved by overridding
    /// <see cref="OnWriteMasthead"/>, <see cref="OnWriteHeader"/>, <see cref="OnWriteArgs(MigratorConsoleArgs)"/> and <see cref="OnWriteFooter(long)"/>.
    /// <para>The underlying command line parsing is provided by <see href="https://natemcmaster.github.io/CommandLineUtils/"/>.</para></remarks>
    public abstract class MigratorConsoleBase
    {
        private const string EntryAssemblyOnlyOptionName = "EO";
        private CommandArgument<MigrationCommand>? _commandArg;
        private CommandArgument? _additionalArgs;
        private readonly Dictionary<string, CommandOption?> _options = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="MigratorConsoleBase"/> class.
        /// </summary>
        /// <param name="args">The default <see cref="MigratorConsoleArgs"/> that will be overridden/updated by the command-line argument values.</param>
        protected MigratorConsoleBase(MigratorConsoleArgs? args = null) => Args = args ?? new MigratorConsoleArgs();

        /// <summary>
        /// Gets the <see cref="MigratorConsoleArgs"/>.
        /// </summary>
        public MigratorConsoleArgs Args { get; }

        /// <summary>
        /// Gets the application/command name.
        /// </summary>
        public virtual string AppName => (Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()).GetName()?.Name ?? "UNKNOWN";

        /// <summary>
        /// Gets the application/command title. 
        /// </summary>
        public virtual string AppTitle => $"{AppName} Database Tool.";

        /// <summary>
        /// Gets the <see cref="Args"/> <see cref="MigratorConsoleArgs.Logger"/>.
        /// </summary>
        protected ILogger? Logger => Args.Logger;

        /// <summary>
        /// Indicates whether to bypass standard execution of <see cref="OnWriteMasthead"/>, <see cref="OnWriteHeader"/>, <see cref="OnWriteArgs(MigratorConsoleArgs)"/> and <see cref="OnWriteFooter(long)"/>.
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
        /// Runs the code generation using the passed <paramref name="migrationCommand"/>.
        /// </summary>
        /// <param name="migrationCommand">The <see cref="MigrationCommand"/>.</param>
        /// <returns><b>Zero</b> indicates success; otherwise, unsuccessful.</returns>
        public async Task<int> RunAsync(MigrationCommand migrationCommand) => await RunAsync(migrationCommand.ToString()).ConfigureAwait(false);

        /// <summary>
        /// Runs the code generation using the passed <paramref name="args"/> string.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <returns><b>Zero</b> indicates success; otherwise, unsuccessful.</returns>
        public async Task<int> RunAsync(string? args = null) => await RunAsync(CodeGenConsole.SplitArgumentsIntoArray(args)).ConfigureAwait(false);

        /// <summary>
        /// Runs the code generation using the passed <paramref name="args"/> array.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <returns><b>Zero</b> indicates success; otherwise, unsuccessful.</returns>
        public async Task<int> RunAsync(string[] args)
        {
            Args.Logger ??= new ConsoleLogger(PhysicalConsole.Singleton);
            HandlebarsHelpers.Logger ??= Args.Logger;

            // Set up the app.
            using var app = new CommandLineApplication(PhysicalConsole.Singleton) { Name = AppName, Description = AppTitle };
            app.HelpOption();

            _commandArg = app.Argument<MigrationCommand>("command", "Database migration command.").IsRequired();
            _options.Add(nameof(OnRamp.CodeGeneratorDbArgsBase.ConnectionString), app.Option("-cs|--connection-string", "Database connection string.", CommandOptionType.SingleValue));
            _options.Add(nameof(OnRamp.CodeGeneratorDbArgsBase.ConnectionStringEnvironmentVariableName), app.Option("-cv|--connection-varname", "Database connection string environment variable name.", CommandOptionType.SingleValue));
            _options.Add(nameof(MigratorConsoleArgs.SchemaOrder), app.Option("-so|--schema-order", "Database schema name (multiple can be specified in priority order).", CommandOptionType.MultipleValue));
            _options.Add(nameof(MigratorConsoleArgs.OutputDirectory), app.Option("-o|--output", "Output directory path.", CommandOptionType.MultipleValue).Accepts(v => v.ExistingDirectory("Output directory path does not exist.")));
            _options.Add(nameof(MigratorConsoleArgs.Assemblies), app.Option("-a|--assembly", "Assembly containing embedded resources (multiple can be specified in probing order).", CommandOptionType.MultipleValue));
            _options.Add(EntryAssemblyOnlyOptionName, app.Option("-eo|--entry-assembly-only", "Use the entry assembly only (ignore all other assemblies).", CommandOptionType.NoValue));
            _additionalArgs = app.Argument("args", "Additional arguments; 'Script' arguments (first being the script name) -or- 'Execute' (each a SQL statement to invoke).", multipleValues: true);

            OnBeforeExecute(app);

            // Set up the validation.
            app.OnValidate(ctx =>
            {
                Args.MigrationCommand = _commandArg.ParsedValue;

                // Update the options from command line.
                var so = GetCommandOption(nameof(MigratorConsoleArgs.SchemaOrder));
                if (so.HasValue())
                {
                    Args.SchemaOrder.Clear();
                    Args.SchemaOrder.AddRange(so.Values.Where(x => !string.IsNullOrEmpty(x)).OfType<string>().Distinct());
                }

                UpdateStringOption(nameof(MigratorConsoleArgs.OutputDirectory), v => Args.OutputDirectory = new DirectoryInfo(v));

                var vr = ValidateMultipleValue(nameof(MigratorConsoleArgs.Assemblies), ctx, (ctx, co) => new AssemblyValidator(Args).GetValidationResult(co, ctx));
                if (vr != ValidationResult.Success)
                    return vr;

                UpdateBooleanOption(EntryAssemblyOnlyOptionName, () =>
                {
                    Args.Assemblies.Clear();
                    Args.AddAssembly(Assembly.GetEntryAssembly()!);
                });

                if (_additionalArgs.Values.Count > 0 && !(Args.MigrationCommand.HasFlag(MigrationCommand.Script) || Args.MigrationCommand.HasFlag(MigrationCommand.Execute)))
                    return new ValidationResult($"Additional arguments can only be specified when the command is '{nameof(MigrationCommand.Script)}' or '{nameof(MigrationCommand.Execute)}'.", new string[] { "args" });

                if (Args.MigrationCommand.HasFlag(MigrationCommand.Script))
                {
                    for (int i = 0; i < _additionalArgs.Values.Count; i++)
                    {
                        if (i == 0)
                            Args.ScriptName = _additionalArgs.Values[i];
                        else
                        {
                            Args.ScriptArguments ??= new Dictionary<string, string?>();
                            Args.ScriptArguments.Add($"Param{i}", _additionalArgs.Values[i]);
                        }
                    }
                }

                if (Args.MigrationCommand.HasFlag(MigrationCommand.Execute))
                {
                    for (int i = 0; i < _additionalArgs.Values.Count; i++)
                    {
                        if (string.IsNullOrEmpty(_additionalArgs.Values[i]))
                            continue;

                        Args.ExecuteStatements ??= new List<string>();
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
                return OnValidation(ctx)!;
            });

            // Set up the code generation execution.
            app.OnExecuteAsync(async _ => await RunRunawayAsync().ConfigureAwait(false));

            // Execute the command-line app.
            try
            {
                return await app.ExecuteAsync(args).ConfigureAwait(false);
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
        protected CommandOption GetCommandOption(string option) => _options.GetValueOrDefault(option) ?? throw new InvalidOperationException($"Command option '{option}' does not exist.");

        /// <summary>
        /// Updates the command option from a string option.
        /// </summary>
        private void UpdateStringOption(string option, Action<string?> action)
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
        private void UpdateBooleanOption(string option, Action action)
        {
            var co = GetCommandOption(option);
            if (co != null && co.HasValue())
                action.Invoke();
        }

        /// <summary>
        /// Validate multiple options.
        /// </summary>
        private ValidationResult ValidateMultipleValue(string option, ValidationContext ctx, Func<ValidationContext, CommandOption, ValidationResult> func)
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
        private async Task<int> RunRunawayAsync() /* Method name inspired by: Slade - Run Runaway - https://www.youtube.com/watch?v=gMxcGaAwy-Q */
        {
            try
            {
                // Write header, etc.
                if (!BypassOnWrites)
                {
                    OnWriteMasthead();
                    OnWriteHeader();
                    OnWriteArgs(Args);
                }

                // Run the code generator.
                var sw = Stopwatch.StartNew();
                if (!await OnMigrateAsync().ConfigureAwait(false))
                    return 3;

                // Write footer and exit successfully.
                sw.Stop();
                if (!BypassOnWrites)
                    OnWriteFooter(sw.ElapsedMilliseconds);

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
        /// Invoked to execute the <see cref="DatabaseMigratorBase"/>.
        /// </summary>
        /// <returns><c>true</c> indicates success; otherwise, <c>false</c>.</returns>
        protected abstract Task<bool> OnMigrateAsync();

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
        /// <param name="args">The <see cref="MigratorConsoleArgs"/> to write.</param>
        protected virtual void OnWriteArgs(MigratorConsoleArgs args) => WriteStandardizedArgs(args);

        /// <summary>
        /// Write the <see cref="Args"/> to the <see cref="Logger"/> in a standardized (reusable) manner.
        /// </summary>
        /// <param name="args">The <see cref="MigratorConsoleArgs"/> to write.</param>
        public static void WriteStandardizedArgs(MigratorConsoleArgs args)
        {
            if (args == null || args.Logger == null)
                return;

            args.Logger.LogInformation("{Content}", $"Command = {args.MigrationCommand}");
            args.Logger.LogInformation("{Content}", $"SchemaOrder = {string.Join(", ", args.SchemaOrder.ToArray())}");
            args.Logger.LogInformation("{Content}", $"OutDir = {args.OutputDirectory?.FullName}");
            args.Logger.LogInformation("{Content}", $"Assemblies{(args.Assemblies.Count == 0 ? " = none" : ":")}");
            foreach (var a in args.Assemblies)
            {
                args.Logger.LogInformation("{Content}", $"  {a.FullName}");
            }
        }

        /// <summary>
        /// Invoked to write the footer information to the <see cref="Logger"/>.
        /// </summary>
        /// <param name="elapsedMilliseconds">The elapsed execution time in milliseconds.</param>
        protected virtual void OnWriteFooter(long elapsedMilliseconds)
        {
            Logger?.LogInformation("{Content}", string.Empty);
            Logger?.LogInformation("{Content}", $"{AppName} Complete. [{elapsedMilliseconds}ms]");
            Logger?.LogInformation("{Content}", string.Empty);
        }
    }
}