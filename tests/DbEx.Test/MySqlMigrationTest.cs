using DbEx.Migration;
using DbEx.MySql.Migration;
using DbEx.Postgres.Migration;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System.Threading.Tasks;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace DbEx.Test
{
    [TestFixture]
    [NonParallelizable]
    public class MySqlMigrationTest
    {
        [Test]
        public async Task A120_MigrateAll()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("MySqlDb");
            var l = UnitTest.GetLogger<MySqlMigrationTest>();
            var a = new MigrationArgs(MigrationCommand.DropAndAll, cs) { Logger = l }.AddAssembly<MySqlStuff>();
            using var m = new MySqlMigration(a);
            var r = await m.MigrateAsync().ConfigureAwait(false);
            Assert.IsTrue(r);
        }

        [Test]
        public async Task A120_MigrateReset()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("MySqlDb");
            var l = UnitTest.GetLogger<MySqlMigrationTest>();
            var a = new MigrationArgs(MigrationCommand.ResetAndDatabase, cs) { Logger = l }.AddAssembly<MySqlStuff>();
            a.DataParserArgs.RefDataColumnDefaults.Add("sort_order", i => i);

            using var m = new MySqlMigration(a);
            var r = await m.MigrateAsync().ConfigureAwait(false);
            Assert.IsTrue(r);

            a.MigrationCommand = MigrationCommand.ResetAndData;
            using var m2 = new MySqlMigration(a);

            r = await m2.MigrateAsync().ConfigureAwait(false);
            Assert.IsTrue(r);
        }

        [Test]
        public async Task MySqlInspect()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("MySqlDb");
            var l = UnitTest.GetLogger<MySqlMigrationTest>();
            var a = new MigrationArgs(MigrationCommand.Inspect, cs) { Logger = l };
            a.Parameters.Add("Param0", "unknown");
            a.Parameters.Add("Param1", "gender");
            a.Parameters.Add("Param2", "CONTACT");

            using var m = new MySqlMigration(a);
            var (Success, Output) = await m.MigrateAndLogAsync().ConfigureAwait(false);

            Assert.IsTrue(Success);
            Assert.IsTrue(Output.Length > 0);

            using var sr = MySqlMigration.GetRequiredResourcesStreamReader("MySqlInspect.md", [typeof(MySqlMigrationTest).Assembly]);
            Assert.AreEqual(sr.ReadToEnd(), Output);
        }
    }
}