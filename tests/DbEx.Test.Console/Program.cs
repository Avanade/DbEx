﻿using DbEx.SqlServer.Console;
using System.Threading.Tasks;

namespace DbEx.Test.Console
{
    public class Program 
    {
        internal static Task<int> Main(string[] args) => SqlServerMigrationConsole
            .Create<Program>("Data Source=.;Initial Catalog=DbEx.Console;Integrated Security=True;TrustServerCertificate=true")
            .Configure(c =>
            {
                c.Args.AddAssembly(typeof(DbEx.Test.OutboxConsole.Program).Assembly);
                c.Args.AddSchemaOrder("Test", "Outbox");
                c.Args.DataParserArgs.Parameter("DefaultName", "Bazza")
                                     .RefDataColumnDefault("SortOrder", i => i)
                                     .ColumnDefault("*", "*", "TenantId", _ => "test-tenant");
            })
            .RunAsync(args);
    }
}