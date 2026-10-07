using DbEx.Migration;
using DbEx.Postgres.Console;
using DbEx.Postgres.Migration;
using DbEx.SqlServer.Migration;
using DbEx.Test.PostgresConsole;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NUnit.Framework;
using System;
using System.Data.Common;
using System.Linq;
using System.Collections.Generic;
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
        public async Task A125_MigrateAll_ExtraTypes_RoundTrip()
        {
            await A120_MigrateAll();

            // Read everything back as text; the spatial types are read as WKT (plus SRID) as their default text form is the binary hex.
            var overrides = new Dictionary<string, string>
            {
                ["geom"] = "ST_AsText(\"geom\")",
                ["geom_any"] = "ST_AsText(\"geom_any\")",
                ["geog"] = "ST_AsText(\"geog\")"
            };

            var columns = new[]
            {
                "extra_types_id", "ip_address", "network", "mac_address", "mac_address8", "search_vector", "search_query", "pt", "ln", "seg", "bx", "pth", "poly", "circ",
                "bit_flag", "bit_mask", "bit_varying", "tags", "numbers", "current_mood", "moods", "int_range", "ts_range", "date_range", "int_multirange",
                "attributes", "label", "ci_text", "geom", "geom_any", "geog", "embedding", "half_embedding", "sparse_embedding"
            };

            var sql = $"SELECT {string.Join(", ", columns.Select(c => $"{(overrides.TryGetValue(c, out var e) ? e : $"\"{c}\"::text")} AS \"{c}\""))}, ST_SRID(\"geom\") AS \"geom_srid\" FROM \"public\".\"extra_types\" ORDER BY \"extra_types_id\"";

            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("PostgresDb");
            using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync();

            var rows = await ExtraTypesReader.ReadAsync(conn, sql);

            ExtraTypesReader.AssertRows(rows, "extra_types_id", new Dictionary<string, string?>
            {
                ["extra_types_id"] = "1",
                ["ip_address"] = "192.168.1.10/32",
                ["network"] = "10.1.0.0/16",
                ["mac_address"] = "08:00:2b:01:02:03",
                ["mac_address8"] = "08:00:2b:01:02:03:04:05",
                ["search_vector"] = "'cat':3 'fat':2",
                ["search_query"] = "'fat' & 'rat'",
                ["pt"] = "(1,2)",
                ["ln"] = "{1,2,3}",
                ["seg"] = "[(0,0),(1,1)]",
                ["bx"] = "(1,1),(0,0)",
                ["pth"] = "[(0,0),(1,1),(2,0)]",
                ["poly"] = "((0,0),(4,0),(4,4))",
                ["circ"] = "<(0,0),5>",
                ["bit_flag"] = "1",
                ["bit_mask"] = "10101010",
                ["bit_varying"] = "101",
                ["tags"] = "{a,b,c}",
                ["numbers"] = "{1,2,3}",
                ["current_mood"] = "happy",
                ["moods"] = "{sad,ok}",
                ["int_range"] = "[1,10)",
                ["ts_range"] = "[\"2024-01-01 00:00:00+00\",\"2024-02-01 00:00:00+00\")",
                ["date_range"] = "[2024-01-01,2024-02-01)",
                ["int_multirange"] = "{[1,3),[5,8)}",
                ["attributes"] = "\"a\"=>\"1\", \"b\"=>\"2\"",
                ["label"] = "Top.Science.Astronomy",
                ["ci_text"] = "Hello",
                ["geom"] = "POINT(1 2)",
                ["geom_any"] = "POLYGON((0 0,4 0,4 4,0 0))",
                ["geog"] = "POINT(-122.349 47.651)",
                ["embedding"] = "[1,2,3]",
                ["half_embedding"] = "[1,2,3]",
                ["sparse_embedding"] = "{1:1,3:2}/3",
                ["geom_srid"] = "4326"
            });
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

            Assert.AreEqual("56003", Assert.ThrowsAsync<PostgresException>(() => db.StoredProcedure("sp_throw_authorization_exception").Param("@message", (string)null).NonQueryAsync()).SqlState);
            Assert.AreEqual("56002", Assert.ThrowsAsync<PostgresException>(() => db.StoredProcedure("sp_throw_business_exception").Param("@message", (string)null).NonQueryAsync()).SqlState);
            Assert.AreEqual("56004", Assert.ThrowsAsync<PostgresException>(() => db.StoredProcedure("sp_throw_concurrency_exception").Param("@message", (string)null).NonQueryAsync()).SqlState);
            Assert.AreEqual("56006", Assert.ThrowsAsync<PostgresException>(() => db.StoredProcedure("sp_throw_conflict_exception").Param("@message", (string)null).NonQueryAsync()).SqlState);
            Assert.AreEqual("56007", Assert.ThrowsAsync<PostgresException>(() => db.StoredProcedure("sp_throw_duplicate_exception").Param("@message", (string)null).NonQueryAsync()).SqlState);
            Assert.AreEqual("56005", Assert.ThrowsAsync<PostgresException>(() => db.StoredProcedure("sp_throw_not_found_exception").Param("@message", (string)null).NonQueryAsync()).SqlState);
            Assert.AreEqual("56001", Assert.ThrowsAsync<PostgresException>(() => db.StoredProcedure("sp_throw_validation_exception").Param("@message", (string)null).NonQueryAsync()).SqlState);

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

        [Test]
        public async Task PostgresInspect()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("PostgresDb");
            var l = UnitTest.GetLogger<PostgresMigrationTest>();
            var a = new MigrationArgs(MigrationCommand.Inspect, cs) { Logger = l };
            a.Parameters.Add("Param0", "public");
            a.Parameters.Add("Param1", "unknown");
            a.Parameters.Add("Param2", "gender");
            a.Parameters.Add("Param3", "CONTACT");
            a.Parameters.Add("Param4", "extra_types");

            using var m = new PostgresMigration(a);
            var (Success, Output) = await m.MigrateAndLogAsync().ConfigureAwait(false);

            Assert.IsTrue(Success);
            Assert.IsTrue(Output.Length > 0);

            using var sr = PostgresMigration.GetRequiredResourcesStreamReader("PostgresInspect.md", [typeof(PostgresMigrationTest).Assembly]);
            MarkdownAssert.AreEqual(sr.ReadToEnd(), Output);
        }
    }
}
