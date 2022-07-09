using OnRamp;
using OnRamp.Console;
using System.IO;
using System.Threading.Tasks;

namespace DbEx.Test.OutboxConsole
{
    public class Program
    {
        internal static Task<int> Main(string[] args) 
            => new CodeGenConsole(new CodeGeneratorArgs("Script.yaml", "Config.yaml") { OutputDirectory = new DirectoryInfo(CodeGenConsole.GetBaseExeDirectory()) }.AddAssembly(typeof(DatabaseExtensions).Assembly)).RunAsync(args);
    }
}