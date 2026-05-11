using OnRamp.Utility;
using DbEx.CodeGen.Config;

namespace DbEx.Tooling.Console;

public static class Program
{
    private static void Main(string[] args)
    {
        if (args.Length == 1)
        {
            switch (args[0].ToUpperInvariant())
            {
                case "--GENERATE-JSON-SCHEMA":
                    JsonSchemaGenerator.Generate<CodeGenConfig>("../../schema/dbex.json", "JSON Schema for DbEx code-generation (https://github.com/avanade/dbex).");
                    break;

                case "--GENERATE-DOC-MARKDOWN":
                    // TODO: MarkdownDocumentationGenerator.Generate<CodeGenConfig>()
                    break;
            }
        }
    }
}