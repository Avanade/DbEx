using DbEx.Migration.SqlServer.Internal;
using DbEx.Schema;
using DbEx.SqlServer;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DbEx.Test
{
    public class Tests
    {
        public class SchemaSqlServer : Database<SqlConnection>
        {
            public SchemaSqlServer(string connectionString) : base(() => new SqlConnection(connectionString)) { }
        }


        [Test]
        public async Task Test1()
        {
            using var db = new SchemaSqlServer("Data Source=.;Initial Catalog=NTangleDemo;Integrated Security=True");

            var tables = await db.SelectSchemaAsync(new DbSchemaArgs { RefDataPredicate = t => t.Columns.Any(c => c.Name == "Code") && t.Columns.Any(c => c.Name == "Text") }.UseSqlServerAdditional()).ConfigureAwait(false);
        }

        [Test]
        public void Parse_1()
        {
            var r = SqlServerObjectReader.Read(@"/* comments */ create" + Environment.NewLine
                + "type dbo.Blah { }", "PROC", "TYPE");

            Assert.IsTrue(r.IsValid);
            Assert.AreEqual(r.Type, "TYPE");
            Assert.AreEqual(r.Schema, "dbo");
            Assert.AreEqual(r.Name, "Blah");
        }

        [Test]
        public void Parse_2()
        {
            var r = SqlServerObjectReader.Read(@"/* comments " + Environment.NewLine
                + " comments */ create" + Environment.NewLine
                + "type Blah { }", "PROC", "TYPE");

            Assert.IsTrue(r.IsValid);
            Assert.AreEqual(r.Type, "TYPE");
            Assert.AreEqual(r.Schema, "dbo");
            Assert.AreEqual(r.Name, "Blah");
        }

        [Test]
        public void Parse_3()
        {
            var r = SqlServerObjectReader.Read(@"CREATE" + Environment.NewLine
                + "-- comments" + Environment.NewLine
                + "type -- comments" + Environment.NewLine
                + "[Ref].[Status]", "PROC", "TYPE");

            Assert.IsTrue(r.IsValid);
            Assert.AreEqual(r.Type, "TYPE");
            Assert.AreEqual(r.Schema, "Ref");
            Assert.AreEqual(r.Name, "Status");
        }

        [Test]
        public void Parse_4()
        {
            var r = SqlServerObjectReader.Read(@"CREATE FUNCTION IsOK()", "FUNCTION", "TYPE");

            Assert.IsTrue(r.IsValid);
            Assert.AreEqual(r.Type, "FUNCTION");
            Assert.AreEqual(r.Schema, "dbo");
            Assert.AreEqual(r.Name, "IsOK");
        }
    }
}