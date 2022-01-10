using DbEx.Console;
using System.Threading.Tasks;

namespace DbEx.Test.Console
{
    public class Program 
    {
        internal static Task<int> Main(string[] args) => SqlServerMigratorConsole
            .Create<Program>("Data Source=.;Initial Catalog=DbEx.Console;Integrated Security=True;TrustServerCertificate=true")
            .ConsoleArgs(a =>
            {
                a.DataParserArgs.Parameters.Add("DefaultName", "Bazza");
                a.DataParserArgs.RefDataColumnDefaults.Add("SortOrder", i => i);
            })
            .RunAsync(args);
    }
}