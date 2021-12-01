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
        private CommandArgument? _scriptArgs;
        private readonly Dictionary<string, CommandOption?> _options = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="MigratorConsoleBase"/> class.
        /// </summary>
        /// <param name="name">The application/command name.</param>
        /// <param name="text">The application/command short text.</param>
        /// <param name="description">The application/command description; will default to <paramref name="text"/> when not specified.</param>
        /// <param name="version">The application/command version number.</param>
        /// <param name="args">The default <see cref="MigratorConsoleArgs"/> that will be overridden/updated by the command-line argument values.</param>
        protected MigratorConsoleBase(string name, string text, string? description = null, string? version = null, MigratorConsoleArgs? args = null)
        {
            Args = args ?? new MigratorConsoleArgs();
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Description = description ?? Text;
            Version = version;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MigratorConsoleBase"/> class defaulting <see cref="Name"/> (with <see cref="AssemblyName.Name"/>), <see cref="Text"/> (with <see cref="AssemblyProductAttribute.Product"/>),
        /// <see cref="Description"/> (with <see cref="AssemblyDescriptionAttribute.Description"/>), and <see cref="Version"/> (with <see cref="AssemblyName.Version"/>) from the <paramref name="assembly"/> where not expressly provided.
        /// </summary>
        /// <param name="args">The default <see cref="MigratorConsoleArgs"/> that will be overridden/updated by the command-line argument values.</param>
        /// <param name="assembly">The <see cref="Assembly"/> to infer properties where not expressly provided.</param>
        /// <param name="name">The application/command name; defaults to <see cref="AssemblyName.Name"/>.</param>
        /// <param name="text">The application/command short text.</param>
        /// <param name="description">The application/command description; defaults to <paramref name="text"/> when not specified.</param>
        /// <param name="version">The application/command version number.</param>
        protected MigratorConsoleBase(Assembly assembly, MigratorConsoleArgs? args = null, string? name = null, string? text = null, string? description = null, string? version = null)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            Args = args ?? new MigratorConsoleArgs();
            var an = assembly.GetName();
            Name = name ?? an?.Name ?? throw new ArgumentException("Unable to infer name.", nameof(name));
            Text = text ?? assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? throw new ArgumentException("Unable to infer text.", nameof(text));
            Version = version ?? (assembly ?? throw new ArgumentNullException(nameof(assembly))).GetName()?.Version?.ToString(3);
            Description = description ?? assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? Text;
        }

        /// <summary>
        /// Gets the <see cref="MigratorConsoleArgs"/>.
        /// </summary>
        public MigratorConsoleArgs Args { get; }

        /// <summary>
        /// Gets the application/command name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the application/command short text.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the application/command description.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Gets the application/command version.
        /// </summary>
        public string? Version { get; }

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
        public string? MastheadText { get; set; } = @"
╔╦╗┌┐ ╔═╗─┐ ┬  ╔╦╗┌─┐┌┬┐┌─┐┌┐ ┌─┐┌─┐┌─┐  ╔╦╗┌─┐┌─┐┬  
 ║║├┴┐║╣ ┌┴┬┘   ║║├─┤ │ ├─┤├┴┐├─┤└─┐├┤    ║ │ ││ ││  
═╩╝└─┘╚═╝┴ └─  ═╩╝┴ ┴ ┴ ┴ ┴└─┘┴ ┴└─┘└─┘   ╩ └─┘└─┘┴─┘
";

        /// <summary>
        /// Runs the code generation using the passed <paramref name="args"/> string.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <returns><b>Zero</b> indicates success; otherwise, unsuccessful.</returns>
        public async Task<int> RunAsync(string? args = null) => await RunAsync(CodeGenConsoleBase.SplitArgumentsIntoArray(args)).ConfigureAwait(false);

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
            using var app = new CommandLineApplication(PhysicalConsole.Singleton) { Name = Name, Description = Description };
            app.HelpOption();

            _commandArg = app.Argument<MigrationCommand>("command", "Database migration command.").IsRequired();
            _options.Add(nameof(OnRamp.CodeGeneratorDbArgsBase.ConnectionString), app.Option("-cs|--connection-string", "Database connection string.", CommandOptionType.SingleValue));
            _options.Add(nameof(OnRamp.CodeGeneratorDbArgsBase.ConnectionStringEnvironmentVariableName), app.Option("-cv|--connection-varname", "Database connection string environment variable name.", CommandOptionType.SingleValue));
            _options.Add(nameof(MigratorConsoleArgs.SchemaOrder), app.Option("-so|--schema-order", "Database schema name (multiple can be specified in priority order).", CommandOptionType.MultipleValue));
            _options.Add(nameof(MigratorConsoleArgs.OutputDirectory), app.Option("-o|--output", "Output directory path.", CommandOptionType.MultipleValue).Accepts(v => v.ExistingDirectory("Output directory path does not exist.")));
            _options.Add(nameof(MigratorConsoleArgs.Assemblies), app.Option("-a|--assembly", "Assembly containing embedded resources (multiple can be specified in probing order).", CommandOptionType.MultipleValue));
            _options.Add(EntryAssemblyOnlyOptionName, app.Option("-eo|--entry-assembly-only", "Use the entry assembly only (ignore all other assemblies).", CommandOptionType.NoValue));
            _scriptArgs = app.Argument("script-args", "Arguments for the Script command (first being the script name).", multipleValues: true);

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

                for (int i = 0; i < _scriptArgs.Values.Count; i++)
                {
                    if (i == 0)
                        Args.ScriptName = _scriptArgs.Values[i];
                    else
                    {
                        Args.ScriptParameters ??= new Dictionary<string, string?>();
                        Args.ScriptParameters.Add($"Param{i}", _scriptArgs.Values[i]);
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
                Args.Logger?.LogError(cpex.Message);
                Args.Logger?.LogError(string.Empty);
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
                    Args.Logger?.LogError(gcex.Message);
                    if (gcex.InnerException != null)
                        Args.Logger?.LogError(gcex.InnerException.Message);

                    Args.Logger?.LogError(string.Empty);
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
                Logger?.LogInformation(MastheadText);
        }

        /// <summary>
        /// Invoked to write the header information to the <see cref="Logger"/>.
        /// </summary>
        /// <remarks>Writes the <see cref="Text"/> and <see cref="Version"/>.</remarks>
        protected virtual void OnWriteHeader()
        {
            Logger?.LogInformation($"{Text}{(Version == null ? "" : $" [v{Version}]")}");
            Logger?.LogInformation(string.Empty);
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

            args.Logger.LogInformation($"Command = {args.MigrationCommand}");
            args.Logger.LogInformation($"SchemaOrder = {string.Join(", ", args.SchemaOrder.ToArray())}");
            args.Logger.LogInformation($"OutDir = {args.OutputDirectory?.FullName}");
            args.Logger.LogInformation($"Assemblies{(args.Assemblies.Count == 0 ? " = none" : ":")}");
            foreach (var a in args.Assemblies)
            {
                args.Logger.LogInformation($"  {a.FullName}");
            }
        }

        /// <summary>
        /// Invoked to write the footer information to the <see cref="Logger"/>.
        /// </summary>
        /// <param name="elapsedMilliseconds">The elapsed execution time in milliseconds.</param>
        protected virtual void OnWriteFooter(long elapsedMilliseconds)
        {
            Logger?.LogInformation(string.Empty);
            Logger?.LogInformation($"{Name} Complete. [{elapsedMilliseconds}ms]");
            Logger?.LogInformation(string.Empty);
        }
    }
}