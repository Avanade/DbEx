using DbEx.Migration;
using DbEx.Postgres.Migration;
using DbEx.Test.PostgresConsole;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System.Threading.Tasks;

namespace DbEx.Test
{
    [TestFixture]
    [NonParallelizable]
    public class PostgresMigrationTest
    {
        [Test]
        public async Task A120_MigrateAll()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("PostgresDb");
            var l = UnitTest.GetLogger<PostgresMigrationTest>();
            var a = new MigrationArgs(MigrationCommand.DropAndAll, cs) { Logger = l }.AddAssembly<PostgresStuff>();
            using var m = new PostgresMigration(a);
            var r = await m.MigrateAsync().ConfigureAwait(false);
            Assert.IsTrue(r);
        }

        [Test]
        public async Task A120_MigrateReset()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("PostgresDb");
            var l = UnitTest.GetLogger<PostgresMigrationTest>();
            var a = new MigrationArgs(MigrationCommand.ResetAndDatabase, cs) { Logger = l }.AddAssembly<PostgresStuff>();
            a.DataParserArgs.RefDataColumnDefaults.Add("sort_order", i => i);

            using var m = new PostgresMigration(a);
            var r = await m.MigrateAsync().ConfigureAwait(false);
            Assert.IsTrue(r);

            a.MigrationCommand = MigrationCommand.ResetAndData;
            using var m2 = new PostgresMigration(a);

            r = await m2.MigrateAsync().ConfigureAwait(false);
            Assert.IsTrue(r);
        }
    }
}