using DbEx.Migration;
using DbEx.Postgres.Console;
using DbEx.Postgres.Migration;
using DbEx.Test.PostgresConsole;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NUnit.Framework;
using System;
using System.Data.Common;
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

            Assert.AreEqual(Assert.ThrowsAsync<PostgresException>(() => db.StoredProcedure("sp_throw_authorization_exception").Param("@message", (string)null).NonQueryAsync()).SqlState, "56003");
            Assert.AreEqual(Assert.ThrowsAsync<PostgresException>(() => db.StoredProcedure("sp_throw_business_exception").Param("@message", (string)null).NonQueryAsync()).SqlState, "56002");
            Assert.AreEqual(Assert.ThrowsAsync<PostgresException>(() => db.StoredProcedure("sp_throw_concurrency_exception").Param("@message", (string)null).NonQueryAsync()).SqlState, "56004");
            Assert.AreEqual(Assert.ThrowsAsync<PostgresException>(() => db.StoredProcedure("sp_throw_conflict_exception").Param("@message", (string)null).NonQueryAsync()).SqlState, "56006");
            Assert.AreEqual(Assert.ThrowsAsync<PostgresException>(() => db.StoredProcedure("sp_throw_duplicate_exception").Param("@message", (string)null).NonQueryAsync()).SqlState, "56007");
            Assert.AreEqual(Assert.ThrowsAsync<PostgresException>(() => db.StoredProcedure("sp_throw_not_found_exception").Param("@message", (string)null).NonQueryAsync()).SqlState, "56005");
            Assert.AreEqual(Assert.ThrowsAsync<PostgresException>(() => db.StoredProcedure("sp_throw_validation_exception").Param("@message", (string)null).NonQueryAsync()).SqlState, "56001");

            var vex = Assert.ThrowsAsync<PostgresException>(() => db.StoredProcedure("sp_throw_validation_exception").Param("@message", "On no!").NonQueryAsync());
            Assert.AreEqual("On no!", vex.MessageText.TrimEnd());
        }

        [Test]
        public async Task B120_Set_Session_Context()
        {
            await A120_MigrateAll();

            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("PostgresDb");
            using var db = new PostgresDatabase(() => new Npgsql.NpgsqlConnection(cs));

            var now = DateTimeOffset.UtcNow;
            var ts = new DateTimeOffset(2024, 09, 30, 23, 45, 08, 123, TimeSpan.FromHours(8));
            var tsUtc = ts.ToUniversalTime();

            await db.StoredProcedure("\"public\".\"sp_set_session_context\"")
                .Param("@Username", "bob@gmail.com")
                .Param("@Timestamp", ts)
                .Param("@TenantId", "banana")
                .Param("@UserId", "bob2")
                .NonQueryAsync().ConfigureAwait(false);

            Assert.That(await db.SqlStatement("select fn_get_timestamp()").ScalarAsync<DateTimeOffset>(), Is.EqualTo(tsUtc));
            Assert.That(await db.SqlStatement("select fn_get_username()").ScalarAsync<string>(), Is.EqualTo("bob@gmail.com"));
            Assert.That(await db.SqlStatement("select fn_get_tenant_id()").ScalarAsync<string>(), Is.EqualTo("banana"));
            Assert.That(await db.SqlStatement("select fn_get_user_id()").ScalarAsync<string>(), Is.EqualTo("bob2"));

            // Make sure the session context doesn't leak between connections.
            using var db2 = new PostgresDatabase(() => new Npgsql.NpgsqlConnection(cs));
            Assert.That(await db2.SqlStatement("select fn_get_timestamp()").ScalarAsync<DateTimeOffset>(), Is.GreaterThanOrEqualTo(now));
            Assert.That(await db2.SqlStatement("select fn_get_username()").ScalarAsync<string>(), Is.Not.Null.And.Not.EqualTo("bob@gmail.com"));
            Assert.That(await db2.SqlStatement("select fn_get_tenant_id()").ScalarAsync<string>(), Is.Null);
            Assert.That(await db2.SqlStatement("select fn_get_user_id()").ScalarAsync<string>(), Is.Null);
        }
    }
}