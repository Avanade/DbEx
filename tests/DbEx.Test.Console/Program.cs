using DbEx.SqlServer.Console;
using System.Threading.Tasks;

namespace DbEx.Test.Console
{
    public class Program 
    {
        internal static Task<int> Main(string[] args) => SqlServerMigrationConsole
            .Create<Program>("Data Source=.;Initial Catalog=DbEx.Console;Integrated Security=True;TrustServerCertificate=true")
            .Configure(c =>
            {
                c.Args.AddAssembly<DbEx.Test.OutboxConsole.Program>("Data", "Data2");
                c.Args.AddSchemaOrder("Test", "Outbox");
                c.Args.IncludeExtendedSchemaScripts();
                c.Args.DataParserArgs.Parameter("DefaultName", "Bazza")
                                     .RefDataColumnDefault("SortOrder", i => i)
                                     .ColumnDefault("*", "*", "TenantId", _ => "test-tenant")
                                     .TableNameMappings.Add("XTest", "XContactType", "Test", "ContactType", new() { { "XNumber", "Number" } })
                                                       .Add("Test", "Addresses", "Test", "ContactAddress");
            })
            .RunAsync(args);
    }
}