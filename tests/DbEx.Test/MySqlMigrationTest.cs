using DbEx.Migration;
using DbEx.MySql.Migration;
using DbEx.Postgres.Migration;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using System.Collections.Generic;
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
        public async Task A125_MigrateAll_ExtraTypes_RoundTrip()
        {
            await A120_MigrateAll();

            // Read everything back as text; the bit, spatial and vector types are read via functions as their default text form is binary.
            const string sql = "SELECT CAST(extra_types_id AS CHAR) AS extra_types_id, CAST(medium_value AS CHAR) AS medium_value, CAST(year_value AS CHAR) AS year_value, " +
                "CAST(bit_flag + 0 AS CHAR) AS bit_flag, BIN(bit_mask) AS bit_mask, enum_value, set_value, " +
                "ST_AsText(geom) AS geom, ST_AsText(pt) AS pt, ST_AsText(ln) AS ln, ST_AsText(poly) AS poly, ST_AsText(multi_pt) AS multi_pt, ST_AsText(multi_ln) AS multi_ln, " +
                "ST_AsText(multi_poly) AS multi_poly, ST_AsText(geom_coll) AS geom_coll, ST_AsText(geog, 'axis-order=long-lat') AS geog, CAST(ST_SRID(geog) AS CHAR) AS geog_srid, " +
                "VECTOR_TO_STRING(embedding) AS embedding FROM extra_types ORDER BY extra_types_id";

            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("MySqlDb");
            using var conn = new MySqlConnection(cs);
            await conn.OpenAsync();

            var rows = await ExtraTypesReader.ReadAsync(conn, sql);

            ExtraTypesReader.AssertRows(rows, "extra_types_id", new Dictionary<string, string?>
            {
                ["extra_types_id"] = "1",
                ["medium_value"] = "100000",
                ["year_value"] = "2024",
                ["bit_flag"] = "1",
                ["bit_mask"] = "10101010",
                ["enum_value"] = "a",
                ["set_value"] = "x,z",
                ["geom"] = "POINT(1 2)",
                ["pt"] = "POINT(1 2)",
                ["ln"] = "LINESTRING(0 0,1 1)",
                ["poly"] = "POLYGON((0 0,4 0,4 4,0 4,0 0))",
                ["multi_pt"] = "MULTIPOINT((0 0),(1 1))",
                ["multi_ln"] = "MULTILINESTRING((0 0,1 1),(2 2,3 3))",
                ["multi_poly"] = "MULTIPOLYGON(((0 0,4 0,4 4,0 4,0 0)))",
                ["geom_coll"] = "GEOMETRYCOLLECTION(POINT(1 2))",
                ["geog"] = "POINT(10 20)",
                ["geog_srid"] = "4326",
                ["embedding"] = "[1.00000e+00,2.00000e+00,3.00000e+00]"
            });
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
            a.Parameters.Add("Param3", "extra_types");

            using var m = new MySqlMigration(a);
            var (Success, Output) = await m.MigrateAndLogAsync().ConfigureAwait(false);

            Assert.IsTrue(Success);
            Assert.IsTrue(Output.Length > 0);

            using var sr = MySqlMigration.GetRequiredResourcesStreamReader("MySqlInspect.md", [typeof(MySqlMigrationTest).Assembly]);
            MarkdownAssert.AreEqual(sr.ReadToEnd(), Output);
        }
    }
}