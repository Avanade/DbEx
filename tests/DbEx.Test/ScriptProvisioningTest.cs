#nullable enable

using DbEx.Migration;
using DbEx.MySql.Console;
using DbEx.MySql.Migration;
using DbEx.Postgres.Console;
using DbEx.Postgres.Migration;
using DbEx.SqlServer.Console;
using DbEx.SqlServer.Migration;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using OnRamp.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DbEx.Test;

[TestFixture]
[NonParallelizable]
public class ScriptProvisioningTest
{
    private string _previousDirectory = null!;
    private DirectoryInfo _directory = null!;
    private static Assembly ExternalAssembly => typeof(DbEx.Test.Console.Program).Assembly;

    [SetUp]
    public void SetUp()
    {
        _previousDirectory = Environment.CurrentDirectory;
        _directory = Directory.CreateDirectory(Path.Combine(TestContext.CurrentContext.WorkDirectory, $"script-{Guid.NewGuid():N}"));
        Environment.CurrentDirectory = _directory.FullName;
    }

    [TearDown]
    public void TearDown()
    {
        Environment.CurrentDirectory = _previousDirectory;
        _directory.Delete(true);
    }

    [TestCase("sql", "[Sales].[Widget]")]
    [TestCase("pgsql", "\"Sales\".\"Widget\"")]
    public async Task ExternalAssembly_ExportsSelectedProviderWithYamlDefault(string provider, string identifier)
    {
        File.WriteAllText(Path.Combine(_directory.FullName, "dbex.yaml"), "schema: Sales\ntables: []");
        using var migrator = CreateMigrator(provider, CreateArgs(provider));
        var (success, output) = await migrator.MigrateAndLogAsync();
        Assert.That(success, Is.True, output);
        var file = ExportedFile();
        Assert.That(file.Extension, Is.EqualTo("." + provider));
        Assert.That(file.Name, Does.EndWith($"-create-sales-widget-table.{provider}"));
        Assert.That(File.ReadAllText(file.FullName), Does.Contain(identifier).And.Not.Contain("{{!"));
    }

    [TestCase(null)]
    [TestCase("tables: []")]
    public async Task MissingConfigurationOrSchema_KeepsLiteralSchema(string? yaml)
    {
        if (yaml != null)
            File.WriteAllText(Path.Combine(_directory.FullName, "dbex.yaml"), yaml);
        using var migrator = CreateMigrator("sql", CreateArgs());
        var (success, output) = await migrator.MigrateAndLogAsync();
        Assert.That(success, Is.True, output);
        Assert.That(File.ReadAllText(ExportedFile().FullName), Does.Contain("[Schema].[Widget]"));
    }

    [TestCase("Sales", "Orders")]
    [TestCase("Schema", "Orders")]
    public async Task ExplicitInputs_WinWithoutReadingMalformedYaml(string schema, string table)
    {
        File.WriteAllText(Path.Combine(_directory.FullName, "dbex.yaml"), "schema: [");
        var supplied = new Dictionary<string, string?> { ["Param1"] = schema, ["Param2"] = table };
        using var migrator = CreateMigrator("sql", CreateArgs());
        Assert.That(await migrator.CreateScriptAsync("wIdGeT", supplied), Is.True);
        Assert.That(File.ReadAllText(ExportedFile().FullName), Does.Contain($"[{schema}].[{table}]"));
        Assert.That(supplied.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task ExplicitLiteralSchema_IsNotReplacedByYaml()
    {
        File.WriteAllText(Path.Combine(_directory.FullName, "dbex.yaml"), "schema: Sales");
        using var migrator = CreateMigrator("sql", CreateArgs());
        Assert.That(await migrator.CreateScriptAsync("widget", new Dictionary<string, string?> { ["Param1"] = "Schema" }), Is.True);
        Assert.That(File.ReadAllText(ExportedFile().FullName), Does.Contain("[Schema].[Widget]"));
    }

    [TestCase("schema: [")]
    [TestCase("schema:")]
    [TestCase("schema: ' '")]
    [TestCase("schema: true")]
    [TestCase("schema: 123")]
    [TestCase("schema: [Sales]")]
    [TestCase("schema: {name: Sales}")]
    [TestCase("schema: Sales\nschema: Other")]
    [TestCase("- Sales")]
    [TestCase("Sales")]
    [TestCase("")]
    [TestCase("schema: Sales\n---\nschema: Other")]
    public async Task InvalidNeededConfiguration_FailsWithoutOutput(string yaml)
    {
        var path = Path.Combine(_directory.FullName, "dbex.yaml");
        File.WriteAllText(path, yaml);
        using var migrator = CreateMigrator("sql", CreateArgs());
        var (success, output) = await migrator.MigrateAndLogAsync();
        Assert.That(success, Is.False);
        Assert.That(output, Does.Contain(path));
        Assert.That(Directory.Exists(Path.Combine(_directory.FullName, "Migrations")), Is.False);
    }

    [Test]
    public async Task UnreadableNeededConfiguration_DoesNotFallBackToLiteralSchema()
    {
        var path = Path.Combine(_directory.FullName, "dbex.yaml");
        Directory.CreateDirectory(path);
        using var migrator = CreateMigrator("sql", CreateArgs());
        var (success, output) = await migrator.MigrateAndLogAsync();
        Assert.That(success, Is.False);
        Assert.That(output, Does.Contain(path).And.Contain("Cannot read"));
        Assert.That(Directory.Exists(Path.Combine(_directory.FullName, "Migrations")), Is.False);
    }

    [Test]
    public async Task Defaults_DoNotMutateCallerParameters()
    {
        var supplied = new Dictionary<string, string?>();
        using var migrator = CreateMigrator("sql", CreateArgs());
        Assert.That(await migrator.CreateScriptAsync("widget", supplied), Is.True);
        Assert.That(supplied, Is.Empty);
    }

    [TestCase("sql", "create", "[Schema].[Name]", "-create-schema-name-table.sql")]
    [TestCase("pgsql", "create", "\"Schema\".\"Name\"", "-create-schema-name-table.pgsql")]
    [TestCase("mysql", "create", "`Name`", "-create-name-table.mysql")]
    [TestCase("sql", "outbox", "[Schema].[Outbox]", "-create-schema-outbox-tables.sql")]
    [TestCase("pgsql", "outbox", "\"Schema\".\"Outbox\"", "-create-schema-outbox-tables.pgsql")]
    [TestCase("sql", "schema", "[Name]", "-create-name-schema.sql")]
    [TestCase("pgsql", "schema", "\"Name\"", "-create-name-schema.pgsql")]
    public async Task BuiltInDefaults_ArePreserved(string provider, string name, string identifier, string filename)
    {
        using var migrator = CreateMigrator(provider, CreateArgs(provider, false));
        Assert.That(await migrator.CreateScriptAsync(name), Is.True);
        var file = ExportedFile();
        Assert.That(file.Name, Does.EndWith(filename));
        Assert.That(File.ReadAllText(file.FullName), Does.Contain(identifier));
    }

    [Test]
    public async Task NonSchemaDefault_DoesNotReadConfiguration()
    {
        File.WriteAllText(Path.Combine(_directory.FullName, "dbex.yaml"), "schema: [");
        using var migrator = CreateMigrator("sql", CreateArgs(external: false));
        Assert.That(await migrator.CreateScriptAsync("schema"), Is.True);
        Assert.That(File.ReadAllText(ExportedFile().FullName), Does.Contain("[Name]"));
    }

    [TestCase("sql")]
    [TestCase("pgsql")]
    [TestCase("mysql")]
    public async Task DefaultScript_StillWorks(string provider)
    {
        using var migrator = CreateMigrator(provider, CreateArgs(provider, false));
        Assert.That(await migrator.CreateScriptAsync(), Is.True);
        Assert.That(ExportedFile().Name, Does.EndWith($"-script-comment-text.{provider}"));
    }

    [TestCase("sql")]
    [TestCase("pgsql")]
    public async Task Help_LoadsCliAssemblyAndOutputWithoutConnectionOrExport(string provider)
    {
        File.WriteAllText(Path.Combine(_directory.FullName, "dbex.yaml"), "schema: Sales");
        var logger = new RecordingLogger();
        var args = new MigrationArgs { Logger = logger };
        DbEx.Console.MigrationConsoleBase console = provider == "sql" ? new SqlServerMigrationConsole(args) : new PostgresMigrationConsole(args);
        var result = await console.RunAsync(["--help", "--assembly", ExternalAssembly.Location, "--output", _directory.FullName]);
        Assert.That(result, Is.Zero, logger.Output);
        Assert.That(logger.Output, Does.Contain("script widget <Schema> <Table>").And.Contain("external").And.Contain("Param1=Sales (dbex.yaml schema)").And.Contain("Param2=Widget"));
        Assert.That(logger.Output, Does.Contain("script [default]"));
        Assert.That(args.ConnectionString, Is.Null.Or.Empty);
        Assert.That(args.DatabaseMigrator, Is.Null);
        Assert.That(Directory.Exists(Path.Combine(_directory.FullName, "Migrations")), Is.False);
        Assert.That(Directory.GetFiles(_directory.FullName), Has.Length.EqualTo(1));
    }

    [Test]
    public async Task Help_EntryAssemblyOnlyRemovesRegisteredExternalTemplates()
    {
        var logger = new RecordingLogger();
        var args = new MigrationArgs { Logger = logger, OutputDirectory = _directory }.AddAssembly(ExternalAssembly);
        var result = await new SqlServerMigrationConsole(args).RunAsync(["--help", "--entry-assembly-only"]);
        Assert.That(result, Is.Zero, logger.Output);
        Assert.That(logger.Output, Does.Not.Contain("script widget").And.Contain("script create"));
    }

    [TestCase("-h")]
    [TestCase("-?")]
    public async Task Help_AliasesParsePrecedingResourceOptions(string option)
    {
        var logger = new RecordingLogger();
        var args = new MigrationArgs { Logger = logger };
        Assert.That(await new SqlServerMigrationConsole(args).RunAsync(["-a", ExternalAssembly.Location, "-o", _directory.FullName, option]), Is.Zero, logger.Output);
        Assert.That(logger.Output, Does.Contain("script widget"));
        Assert.That(args.OutputDirectory?.FullName, Is.EqualTo(_directory.FullName));
    }

    [Test]
    public async Task Help_ExplicitSchemaAvoidsReadingMalformedYaml()
    {
        File.WriteAllText(Path.Combine(_directory.FullName, "dbex.yaml"), "schema: [");
        var logger = new RecordingLogger();
        var args = new MigrationArgs { Logger = logger, OutputDirectory = _directory };
        Assert.That(await new SqlServerMigrationConsole(args).RunAsync(["--help", "-p", "Param1=Sales"]), Is.Zero, logger.Output);
        Assert.That(logger.Output, Does.Contain("Param1=Sales"));
    }

    [Test]
    public async Task Help_MySqlListsOnlyItsProviderTemplates()
    {
        var logger = new RecordingLogger();
        var args = new MigrationArgs { Logger = logger, OutputDirectory = _directory }.AddAssembly(ExternalAssembly);
        Assert.That(await new MySqlMigrationConsole(args).RunAsync("--help"), Is.Zero, logger.Output);
        Assert.That(logger.Output, Does.Contain("script alter <table>").And.Contain("script create <table>").And.Contain("script refdata <table>")
            .And.Not.Contain("script schema").And.Not.Contain("script outbox").And.Not.Contain("script widget"));
        Assert.That(args.DatabaseMigrator, Is.Null);
    }

    [TestCase("--assembly", "Definitely.Not.An.Assembly")]
    [TestCase("--output", "definitely-missing-directory")]
    public async Task Help_InvalidOptionsFailVisibly(string option, string value)
    {
        var logger = new RecordingLogger();
        var args = new MigrationArgs { Logger = logger };
        Assert.That(await new SqlServerMigrationConsole(args).RunAsync(["--help", option, value]), Is.Not.Zero);
        Assert.That(logger.Output, Does.Contain(value));
    }

    [Test]
    public async Task Help_InvalidNeededYamlFailsVisibly()
    {
        File.WriteAllText(Path.Combine(_directory.FullName, "dbex.yaml"), "schema: [");
        var logger = new RecordingLogger();
        var args = new MigrationArgs { Logger = logger, OutputDirectory = _directory };
        Assert.That(await new SqlServerMigrationConsole(args).RunAsync("--help"), Is.Not.Zero);
        Assert.That(logger.Output, Does.Contain("dbex.yaml").And.Contain("invalid"));
    }

    [TestCase("script widget Sales Orders", true)]
    [TestCase("script widget -p Param1=Sales -p Param2=Orders", true)]
    [TestCase("script widget -p Param1=Other -p Param1=Sales -p Param2=Orders", true)]
    [TestCase("script widget Sales Orders -p Param1=Sales", true)]
    [TestCase("script widget Sales Orders -p Param1=Other", false)]
    public async Task CliInputs_ArePositionalOrNamedWithExplicitConflicts(string command, bool expected)
    {
        var logger = new RecordingLogger();
        var args = CreateArgs();
        args.Logger = logger;
        var result = await new SqlServerMigrationConsole(args).RunAsync(command);
        Assert.That(result == 0, Is.EqualTo(expected), logger.Output);
        if (expected)
            Assert.That(File.ReadAllText(ExportedFile().FullName), Does.Contain("[Sales].[Orders]"));
        else
        {
            Assert.That(Directory.Exists(Path.Combine(_directory.FullName, "Migrations")), Is.False);
        }
    }

    [Test]
    public async Task RegisteredAssembly_OverridesBuiltInAcrossExtensions()
    {
        var logger = new RecordingLogger();
        var args = CreateArgs();
        args.Logger = logger;
        Assert.That(await new SqlServerMigrationConsole(args).RunAsync("--help"), Is.Zero, logger.Output);
        Assert.That(logger.Output, Does.Contain("External alter override").And.Not.Contain("perform an ALTER TABLE"));
        using var migrator = CreateMigrator("sql", args);
        Assert.That(await migrator.CreateScriptAsync("alter"), Is.True);
        Assert.That(File.ReadAllText(ExportedFile().FullName), Does.Contain("External alter override"));
    }

    [Test]
    public async Task RegisteredAssembly_OverridesBuiltInAfterDefaultAssembliesAreCleared()
    {
        var logger = new RecordingLogger();
        var args = CreateArgs(external: false);
        args.ClearAssemblies();
        args.AddAssembly(ExternalAssembly);
        args.Logger = logger;
        Assert.That(await new SqlServerMigrationConsole(args).RunAsync("--help"), Is.Zero, logger.Output);
        Assert.That(logger.Output, Does.Contain("External alter override").And.Not.Contain("perform an ALTER TABLE"));
        using var migrator = CreateMigrator("sql", args);
        Assert.That(await migrator.CreateScriptAsync("alter"), Is.True);
        Assert.That(File.ReadAllText(ExportedFile().FullName), Does.Contain("External alter override"));
    }

    [Test]
    public async Task CliAssemblyOption_ExportsExternalTemplate()
    {
        var logger = new RecordingLogger();
        var args = CreateArgs(external: false);
        args.Logger = logger;
        var result = await new SqlServerMigrationConsole(args).RunAsync(
            ["script", "widget", "Sales", "Orders", "--assembly", ExternalAssembly.Location]);
        Assert.That(result, Is.Zero, logger.Output);
        Assert.That(File.ReadAllText(ExportedFile().FullName), Does.Contain("[Sales].[Orders]"));
    }

    [Test]
    public async Task EntryAssemblyOnly_UsesEntryTemplateForHelpAndExport()
    {
        async Task<(int ExitCode, string Output)> RunConsoleAsync(params string[] arguments)
        {
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(ExternalAssembly.Location);
            foreach (var argument in arguments)
                start.ArgumentList.Add(argument);
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync();
            var errors = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            return (process.ExitCode, (await output) + (await errors));
        }

        var help = await RunConsoleAsync("--help", "--entry-assembly-only", "--output", _directory.FullName);
        Assert.That(help.ExitCode, Is.Zero, help.Output);
        Assert.That(help.Output, Does.Contain("External alter override"));

        var export = await RunConsoleAsync("script", "alter", "Sales", "Orders", "--entry-assembly-only", "--output", _directory.FullName);
        Assert.That(export.ExitCode, Is.Zero, export.Output);
        Assert.That(File.ReadAllText(ExportedFile().FullName), Does.Contain("External alter override: [Sales].[Orders]"));
    }

    [TestCase("sql")]
    [TestCase("pgsql")]
    public async Task LastRegisteredAssembly_WinsForHelpAndExport(string provider)
    {
        var logger = new RecordingLogger();
        var args = CreateArgs(provider).AddAssembly(typeof(ScriptProvisioningTest).Assembly);
        args.Logger = logger;
        DbEx.Console.MigrationConsoleBase console = provider == "sql" ? new SqlServerMigrationConsole(args) : new PostgresMigrationConsole(args);
        Assert.That(await console.RunAsync("--help"), Is.Zero, logger.Output);
        Assert.That(logger.Output, Does.Contain("Widget override from the last registered assembly").And.Not.Contain("external SQL Server widget").And.Not.Contain("external PostgreSQL widget"));
        using var migrator = CreateMigrator(provider, args);
        Assert.That(await migrator.CreateScriptAsync("widget"), Is.True);
        Assert.That(File.ReadAllText(ExportedFile().FullName), Does.Contain("Second assembly:").And.Contain("SecondWidget"));
    }

    [Test]
    public async Task AmbiguousResourcesInSameAssembly_FailHelpAndExport()
    {
        var logger = new RecordingLogger();
        var args = CreateArgs().AddAssembly(typeof(DbEx.Test.Error.TestError).Assembly);
        args.Logger = logger;
        Assert.That(await new SqlServerMigrationConsole(args).RunAsync("--help"), Is.Not.Zero);
        Assert.That(logger.Output, Does.Contain("ambiguous").And.Contain("First.Resources.").And.Contain("Second.Resources."));
        using var migrator = CreateMigrator("sql", args);
        Assert.That(await migrator.CreateScriptAsync("ambiguous"), Is.False);
        Assert.That(Directory.Exists(Path.Combine(_directory.FullName, "Migrations")), Is.False);
    }

    [Test]
    public async Task LocalResources_UseSameWinnerAndPreserveEarlierSnapshot()
    {
        var resources = Directory.CreateDirectory(Path.Combine(_directory.FullName, "Resources"));
        var path = Path.Combine(resources.FullName, "ScriptWidget_sql.hbs");
        const string template = """
            {{! COMMAND: script widget <Schema> <Table> }}
            {{! HELP: Local widget. }}
            {{! FILENAME:[lookup Parameters 'Param2'] }}
            {{! PARAM:Param1=Schema }}
            {{! PARAM:Param2=First }}
            -- Local version one: {{lookup Parameters 'Param1'}}.{{lookup Parameters 'Param2'}}
            """;
        File.WriteAllText(path, template);
        var args = CreateArgs();
        var logger = new RecordingLogger();
        args.Logger = logger;
        Assert.That(await new SqlServerMigrationConsole(args).RunAsync("--help"), Is.Zero, logger.Output);
        Assert.That(logger.Output, Does.Contain("Local widget.").And.Not.Contain("external SQL Server widget"));
        using var migrator = CreateMigrator("sql", args);
        Assert.That(await migrator.CreateScriptAsync("widget"), Is.True);
        var original = ExportedFile();
        var bytes = File.ReadAllBytes(original.FullName);
        File.WriteAllText(path, template.Replace("version one", "version two").Replace("=First", "=Second"));
        Assert.That(await migrator.CreateScriptAsync("widget"), Is.True);
        Assert.That(File.ReadAllBytes(original.FullName), Is.EqualTo(bytes));
        var later = new DirectoryInfo(original.DirectoryName!).GetFiles().Single(x => x.FullName != original.FullName);
        Assert.That(File.ReadAllText(later.FullName), Does.Contain("Local version two: Schema.Second"));
    }

    [Test]
    public async Task LocalHbWinsOverHbs_AndCurrentDirectoryWinsOverResources()
    {
        Directory.CreateDirectory(Path.Combine(_directory.FullName, "Resources"));
        File.WriteAllText(Path.Combine(_directory.FullName, "Resources", "ScriptWidget_sql.hb"), "-- resources");
        File.WriteAllText(Path.Combine(_directory.FullName, "ScriptWidget_sql.hbs"), "-- current hbs");
        File.WriteAllText(Path.Combine(_directory.FullName, "ScriptWidget_sql.hb"), "-- current hb");
        using var migrator = CreateMigrator("sql", CreateArgs());
        Assert.That(await migrator.CreateScriptAsync("widget"), Is.True);
        Assert.That(File.ReadAllText(ExportedFile().FullName), Is.EqualTo("-- current hb"));
    }

    [TestCase("absent")]
    [TestCase("../widget")]
    public async Task UnknownOrUnprefixedTemplates_AreNotExported(string name)
    {
        File.WriteAllText(Path.Combine(_directory.FullName, "absent_sql.hbs"), "-- not a Script template");
        using var migrator = CreateMigrator("sql", CreateArgs());
        var args = migrator.Args;
        args.Parameters["Param0"] = name;
        var (success, output) = await migrator.MigrateAndLogAsync();
        Assert.That(success, Is.False);
        Assert.That(output, Does.Contain("--help"));
        Assert.That(Directory.Exists(Path.Combine(_directory.FullName, "Migrations")), Is.False);
    }

    [Test]
    public async Task LegacyMetadata_PreservesEqualsAndNullDefaults()
    {
        File.WriteAllText(Path.Combine(_directory.FullName, "ScriptLegacy_sql.hbs"), """
            {{! PARAM:Param1=a=b }}
            {{! PARAM:Param2 }}
            {{! PARAM:Param3= }}
            -- {{lookup Parameters 'Param1'}} / {{lookup Parameters 'Param2'}}
            """);
        var logger = new RecordingLogger();
        var args = new MigrationArgs { Logger = logger, OutputDirectory = _directory };
        Assert.That(await new SqlServerMigrationConsole(args).RunAsync("--help"), Is.Zero);
        Assert.That(logger.Output, Does.Contain("script legacy").And.Contain("Param1=a=b"));
        Assert.That(logger.Output, Does.Contain("Param2=(null)").And.Contain("Param3=(null)"));
        using var migrator = CreateMigrator("sql", CreateArgs());
        Assert.That(await migrator.CreateScriptAsync("legacy"), Is.True);
        Assert.That(File.ReadAllText(ExportedFile().FullName), Does.Contain("-- a=b /"));
    }

    [Test]
    public async Task InvalidCommandMetadata_FailsHelpAndExport()
    {
        File.WriteAllText(Path.Combine(_directory.FullName, "ScriptWidget_sql.hbs"), "{{! COMMAND: script different }}");
        var logger = new RecordingLogger();
        var args = CreateArgs();
        args.Logger = logger;
        Assert.That(await new SqlServerMigrationConsole(args).RunAsync("--help"), Is.Not.Zero);
        Assert.That(logger.Output, Does.Contain("COMMAND").And.Contain("ScriptWidget_sql.hbs"));
        using var migrator = CreateMigrator("sql", args);
        Assert.That(await migrator.CreateScriptAsync("widget"), Is.False);
    }

    [Test]
    public async Task CollidingExport_DoesNotAlterExistingFiles()
    {
        var migrations = Directory.CreateDirectory(Path.Combine(_directory.FullName, "Migrations"));
        var now = DateTime.UtcNow;
        for (var i = -60; i <= 60; i++)
            File.WriteAllText(Path.Combine(migrations.FullName, $"{now.AddSeconds(i):yyyyMMdd-HHmmss}-create-schema-widget-table.sql"), "original snapshot");
        using var migrator = CreateMigrator("sql", CreateArgs());
        var (success, output) = await migrator.MigrateAndLogAsync();
        Assert.That(success, Is.False, output);
        Assert.That(output, Does.Contain("already exists").And.Contain("was not changed"));
        Assert.That(migrations.GetFiles(), Has.Length.EqualTo(121));
        Assert.That(migrations.GetFiles().All(x => File.ReadAllText(x.FullName) == "original snapshot"), Is.True);
    }

    [Test]
    public async Task InvalidRenderedFilename_FailsBeforeCreatingOutput()
    {
        using var migrator = CreateMigrator("sql", CreateArgs());
        Assert.That(await migrator.CreateScriptAsync("widget", new Dictionary<string, string?> { ["Param2"] = @"..\escaped" }), Is.False);
        Assert.That(Directory.Exists(Path.Combine(_directory.FullName, "Migrations")), Is.False);
    }

    [Test]
    public async Task FailedRendering_DoesNotLeaveOutput()
    {
        File.WriteAllText(Path.Combine(_directory.FullName, "ScriptBroken_sql.hbs"), "{{#if}}");
        using var migrator = CreateMigrator("sql", CreateArgs());
        Assert.That(await migrator.CreateScriptAsync("broken"), Is.False);
        Assert.That(Directory.Exists(Path.Combine(_directory.FullName, "Migrations")), Is.False);
    }

    [Test]
    public void CancelledExport_PropagatesCancellationWithoutCreatingOutput()
    {
        using var migrator = CreateMigrator("sql", CreateArgs());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsAsync<OperationCanceledException>(async () => await migrator.CreateScriptAsync("widget", cancellationToken: cancellation.Token));
        Assert.That(Directory.Exists(Path.Combine(_directory.FullName, "Migrations")), Is.False);
    }

    [Test]
    public async Task ProgrammaticOutputDirectory_CanBeCreatedDuringExport()
    {
        var args = CreateArgs();
        args.OutputDirectory = new DirectoryInfo(Path.Combine(_directory.FullName, "Generated"));
        Assert.That(await new SqlServerMigrationConsole(args).RunAsync("script widget"), Is.Zero);
        Assert.That(Directory.GetFiles(Path.Combine(args.OutputDirectory.FullName, "Migrations")), Has.Length.EqualTo(1));
    }

    [Test]
    public void Renderer_DoesNotHtmlEncodeSimpleIdentifierValues()
    {
        var rendered = new HandlebarsCodeGenerator("[{{Name}}]").Generate(new { Name = "Order_History" });
        Assert.That(rendered, Is.EqualTo("[Order_History]"));
        Assert.That(new HandlebarsCodeGenerator("[{{Name}}]").Generate(new { Name = "Order]History&More" }), Is.EqualTo("[Order]History&More]"));
    }

    private MigrationArgs CreateArgs(string provider = "sql", bool external = true)
    {
        var args = new MigrationArgs(MigrationCommand.Script, provider switch
        {
            "sql" => "Server=localhost;Database=NeverConnect;Integrated Security=true;TrustServerCertificate=true",
            "pgsql" => "Host=localhost;Database=NeverConnect;Username=test",
            _ => "Server=localhost;Database=NeverConnect;User ID=test"
        }) { OutputDirectory = _directory };
        if (external)
            args.AddAssembly(ExternalAssembly);
        args.Parameters["Param0"] = "widget";
        return args;
    }

    private static DatabaseMigrationBase CreateMigrator(string provider, MigrationArgs args) => provider switch
    {
        "sql" => new SqlServerMigration(args),
        "pgsql" => new PostgresMigration(args),
        _ => new MySqlMigration(args)
    };

    private FileInfo ExportedFile() => new DirectoryInfo(Path.Combine(_directory.FullName, "Migrations")).GetFiles().Single();

    private sealed class RecordingLogger : ILogger
    {
        private readonly StringBuilder _output = new();
        public string Output => _output.ToString();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => _output.AppendLine(formatter(state, exception));
    }
}
