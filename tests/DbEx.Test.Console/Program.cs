using DbEx.Console.SqlServer;
using System.Threading.Tasks;

namespace DbEx.Test.Console
{
    public class Program 
    {
        internal static Task<int> Main(string[] args) => SqlServerMigrationConsole
            .Create<Program>("Data Source=.;Initial Catalog=DbEx.Console;Integrated Security=True;TrustServerCertificate=true")
            .Configure(c =>
            {
                c.Args.DataParserArgs.Parameters.Add("DefaultName", "Bazza");
                c.Args.DataParserArgs.RefDataColumnDefaults.Add("SortOrder", i => i);
                c.Args.AddAssembly(typeof(DbEx.Test.OutboxConsole.Program).Assembly);
                c.Args.AddSchemaOrder("Test", "Outbox");
            })
            .RunAsync(args);
    }
}