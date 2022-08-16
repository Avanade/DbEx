using CoreEx.Database;
using CoreEx.Database.SqlServer;
using DbEx.Migration.Data;
using DbEx.Migration.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DbEx.Test
{
    [TestFixture]
    [NonParallelizable]
    public class SqlServerMigratorTest
    {
        [Test]
        public async Task A100_MigrateAll_None()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("NoneDb");
            var l = UnitTest.GetLogger<SqlServerMigratorTest>();
            var m = new SqlServerMigrator(cs, Migration.MigrationCommand.DropAndAll, l);
            var r = await m.MigrateAsync().ConfigureAwait(false);

            Assert.IsTrue(r);
        }

        [Test]
        public async Task A110_MigrateAll_Empty()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("EmptyDb");
            var l = UnitTest.GetLogger<SqlServerMigratorTest>();
            var m = new SqlServerMigrator(cs, Migration.MigrationCommand.DropAndAll, l, typeof(Empty.Test).Assembly);
            var r = await m.MigrateAsync().ConfigureAwait(false);

            Assert.IsTrue(r);
        }

        [Test]
        public async Task A120_MigrateAll_Console()
        {
            var (cs, l, m) = await CreateConsoleDb().ConfigureAwait(false);

            // Check that the contact data was updated as expected.
            using var db = new SqlServerDatabase(() => new SqlConnection(cs));
            var res = (await db.SqlStatement("SELECT * FROM [Test].[Contact]").SelectQueryAsync(dr => new
            {
                ContactId = dr.GetValue<int>("ContactId"),
                Name = dr.GetValue<string>("Name"),
                Phone = dr.GetValue<string>("Phone"),
                DateOfBirth = dr.GetValue<DateTime?>("DateOfBirth"),
                ContactTypeId = dr.GetValue<int>("ContactTypeId"),
                GenderId = dr.GetValue<int?>("GenderId")
            }).ConfigureAwait(false)).ToList();

            Assert.AreEqual(3, res.Count);

            var row = res[0];
            Assert.AreEqual(1, row.ContactId);
            Assert.AreEqual("Bob", row.Name);
            Assert.AreEqual(null, row.Phone);
            Assert.AreEqual(new DateTime(2001, 10, 22), row.DateOfBirth);
            Assert.AreEqual(1, row.ContactTypeId);
            Assert.AreEqual(2, row.GenderId);

            row = res[1];
            Assert.AreEqual(2, row.ContactId);
            Assert.AreEqual("Jane", row.Name);
            Assert.AreEqual("1234", row.Phone);
            Assert.AreEqual(null, row.DateOfBirth);
            Assert.AreEqual(2, row.ContactTypeId);
            Assert.IsNull(row.GenderId);

            row = res[2];
            Assert.AreEqual(3, row.ContactId);
            Assert.AreEqual("Barry", row.Name);
            Assert.AreEqual(null, row.Phone);
            Assert.AreEqual(new DateTime(2001, 10, 22), row.DateOfBirth);
            Assert.AreEqual(1, row.ContactTypeId);
            Assert.AreEqual(2, row.GenderId);

            // Check that the person data was updated as expected - converted and auto-assigned id, plus createdby and createddate columns, and finally runtime variable.
            var res2 = (await db.SqlStatement("SELECT * FROM [Test].[Person]").SelectQueryAsync(dr => new
            {
                PersonId = dr.GetValue<Guid>("PersonId"),
                Name = dr.GetValue<string>("Name"),
                CreatedBy = dr.GetValue<string>("CreatedBy"),
                CreatedDate = dr.GetValue<DateTime>("CreatedDate"),
            }).ConfigureAwait(false)).ToList();

            Assert.AreEqual(3, res.Count);
            var row2 = res2[0];
            Assert.AreEqual(DataValueConverter.IntToGuid(88), row2.PersonId);
            Assert.AreEqual("RUNTIME", row2.Name);
            Assert.AreEqual(m.ParserArgs.UserName, row2.CreatedBy);
            Assert.AreEqual(m.ParserArgs.DateTimeNow, row2.CreatedDate);

            row2 = res2[1];
            Assert.AreNotEqual(Guid.Empty, row2.PersonId);
            Assert.AreEqual("Bazza", row2.Name);
            Assert.AreEqual(m.ParserArgs.UserName, row2.CreatedBy);
            Assert.AreEqual(m.ParserArgs.DateTimeNow, row2.CreatedDate);

            // Check that the stored procedure script was migrated and works!
            res = (await db.StoredProcedure("[Test].[spGetContact]").Param("@ContactId", 2).SelectQueryAsync(dr => new
            {
                ContactId = dr.GetValue<int>("ContactId"),
                Name = dr.GetValue<string>("Name"),
                Phone = dr.GetValue<string>("Phone"),
                DateOfBirth = dr.GetValue<DateTime?>("DateOfBirth"),
                ContactTypeId = dr.GetValue<int>("ContactTypeId"),
                GenderId = dr.GetValue<int?>("GenderId")
            }).ConfigureAwait(false)).ToList();

            Assert.AreEqual(1, res.Count);
            row = res[0];
            Assert.AreEqual(2, row.ContactId);
            Assert.AreEqual("Jane", row.Name);
            Assert.AreEqual("1234", row.Phone);
            Assert.AreEqual(null, row.DateOfBirth);
            Assert.AreEqual(2, row.ContactTypeId);
            Assert.IsNull(row.GenderId);
        }

        private static async Task<(string cs, ILogger l, SqlServerMigrator m)> CreateConsoleDb()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("ConsoleDb");
            var l = UnitTest.GetLogger<SqlServerMigratorTest>();
            var m = new SqlServerMigrator(cs, Migration.MigrationCommand.DropAndAll, l, typeof(Console.Program).Assembly);

            m.ParserArgs.Parameters.Add("DefaultName", "Bazza");
            m.ParserArgs.RefDataColumnDefaults.Add("SortOrder", i => i);

            var r = await m.MigrateAsync().ConfigureAwait(false);

            Assert.IsTrue(r);

            return (cs, l, m);
        }

        [Test]
        public async Task B100_Execute_Console_Success()
        {
            var c = await CreateConsoleDb().ConfigureAwait(false);
            var m = new SqlServerMigrator(c.cs, Migration.MigrationCommand.Execute, c.l, typeof(Console.Program).Assembly);

            var r = await m.ExecuteSqlStatementsAsync("SELECT * FROM Test.Contact").ConfigureAwait(false);
            Assert.IsTrue(r);
        }

        [Test]
        public async Task B100_Execute_Console_Error()
        {
            var c = await CreateConsoleDb().ConfigureAwait(false);
            var m = new SqlServerMigrator(c.cs, Migration.MigrationCommand.Execute, c.l, typeof(Console.Program).Assembly);

            var r = await m.ExecuteSqlStatementsAsync("SELECT * FROM Test.Contact", "SELECT BANANAS").ConfigureAwait(false);
            Assert.IsFalse(r);
        }
    }
}