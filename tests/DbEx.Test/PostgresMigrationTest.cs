using CoreEx;
using CoreEx.Database;
using CoreEx.Database.Postgres;
using DbEx.Migration;
using DbEx.Postgres.Console;
using DbEx.Postgres.Migration;
using DbEx.Test.PostgresConsole;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

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
            var a = new MigrationArgs(MigrationCommand.DropAndAll, cs) { Logger = l }.AddAssembly<PostgresStuff>().IncludeExtendedSchemaScripts();
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

        [Test]
        public async Task B110_Throw_Exceptions()
        {
            await A120_MigrateAll();

            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("PostgresDb");
            using var db = new PostgresDatabase(() => new Npgsql.NpgsqlConnection(cs));

            Assert.ThrowsAsync<AuthorizationException>(() => db.StoredProcedure("sp_throw_authorization_exception").Param("message", null).NonQueryAsync());
            Assert.ThrowsAsync<BusinessException>(() => db.StoredProcedure("sp_throw_business_exception").Param("message", null).NonQueryAsync());
            Assert.ThrowsAsync<ConcurrencyException>(() => db.StoredProcedure("sp_throw_concurrency_exception").Param("message", null).NonQueryAsync());
            Assert.ThrowsAsync<ConflictException>(() => db.StoredProcedure("sp_throw_conflict_exception").Param("message", null).NonQueryAsync());
            Assert.ThrowsAsync<DuplicateException>(() => db.StoredProcedure("sp_throw_duplicate_exception").Param("message", null).NonQueryAsync());
            Assert.ThrowsAsync<NotFoundException>(() => db.StoredProcedure("sp_throw_not_found_exception").Param("message", null).NonQueryAsync());
            Assert.ThrowsAsync<ValidationException>(() => db.StoredProcedure("sp_throw_validation_exception").Param("message", null).NonQueryAsync());
            var vex = Assert.ThrowsAsync<ValidationException>(() => db.StoredProcedure("sp_throw_validation_exception").Param("message", "On no!").NonQueryAsync());
            Assert.AreEqual("On no!", vex.Message);
        }

        [Test]
        public async Task B120_Set_Session_Context()
        {
            await A120_MigrateAll();

            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("PostgresDb");
            using var db = new PostgresDatabase(() => new Npgsql.NpgsqlConnection(cs));

            var now = DateTime.UtcNow;
            var ts = new DateTime(2024, 09, 30, 23, 45, 08, 123, DateTimeKind.Utc);

            await db.SetPostgresSessionContextAsync("bob@gmail.com", ts, "banana", "bob2");

            Assert.That(await db.SqlStatement("select fn_get_timestamp()").ScalarAsync<DateTime>(), Is.EqualTo(ts));
            Assert.That(await db.SqlStatement("select fn_get_username()").ScalarAsync<string>(), Is.EqualTo("bob@gmail.com"));
            Assert.That(await db.SqlStatement("select fn_get_tenant_id()").ScalarAsync<string>(), Is.EqualTo("banana"));
            Assert.That(await db.SqlStatement("select fn_get_user_id()").ScalarAsync<string>(), Is.EqualTo("bob2"));

            // Make sure the session context doesn't leak between connections.
            using var db2 = new PostgresDatabase(() => new Npgsql.NpgsqlConnection(cs));
            Assert.That(await db2.SqlStatement("select fn_get_timestamp()").ScalarAsync<DateTime>(), Is.GreaterThanOrEqualTo(now));
            Assert.That(await db2.SqlStatement("select fn_get_username()").ScalarAsync<string>(), Is.Not.Null.And.Not.EqualTo("bob@gmail.com"));
            Assert.That(await db2.SqlStatement("select fn_get_tenant_id()").ScalarAsync<string>(), Is.Null);
            Assert.That(await db2.SqlStatement("select fn_get_user_id()").ScalarAsync<string>(), Is.Null);
        }
    }
}