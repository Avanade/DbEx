using DbEx.Migration.Data;
using DbEx.Migration.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
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
            var cs = UnitTest.GetConfig("DbEx").GetConnectionString("NoneDb");
            var l = UnitTest.GetLogger<SqlServerMigratorTest>();
            var m = new SqlServerMigrator(cs, Migration.MigrationCommand.DropAndAll, l);
            var r = await m.MigrateAsync().ConfigureAwait(false);

            Assert.IsTrue(r);
        }

        [Test]
        public async Task A110_MigrateAll_Empty()
        {
            var cs = UnitTest.GetConfig("DbEx").GetConnectionString("EmptyDb");
            var l = UnitTest.GetLogger<SqlServerMigratorTest>();
            var m = new SqlServerMigrator(cs, Migration.MigrationCommand.DropAndAll, l, typeof(Empty.Test).Assembly);
            var r = await m.MigrateAsync().ConfigureAwait(false);

            Assert.IsTrue(r);
        }

        [Test]
        public async Task A120_MigrateAll_Basic()
        {
            var cs = UnitTest.GetConfig("DbEx").GetConnectionString("BasicDb");
            var l = UnitTest.GetLogger<SqlServerMigratorTest>();
            var m = new SqlServerMigrator(cs, Migration.MigrationCommand.DropAndAll, l, typeof(Basic.Test).Assembly);

            m.ParserArgs.Parameters.Add("DefaultName", "Bazza");
            m.ParserArgs.RefDataColumnDefaults.Add(("SortOrder", (i) => i));

            var r = await m.MigrateAsync().ConfigureAwait(false);

            Assert.IsTrue(r);

            // Check that the contact data was updated as expected.
            var db = new Database<SqlConnection>(() => new SqlConnection(cs));
            var res = (await db.SqlStatement("SELECT * FROM [Test].[Contact]").SelectAsync(dr => new
            {
                ContactId = dr.GetValue<int>("ContactId"),
                Name = dr.GetValue<string>("Name"),
                Phone = dr.GetValue<string>("Phone"),
                DateOfBirth = dr.GetValue<DateTime?>("DateOfBirth"),
                ContactTypeId = dr.GetValue<int>("ContactTypeId")
            }).ConfigureAwait(false)).ToList();

            Assert.AreEqual(2, res.Count);

            var row = res[0];
            Assert.AreEqual(1, row.ContactId);
            Assert.AreEqual("Bob", row.Name);
            Assert.AreEqual(null, row.Phone);
            Assert.AreEqual(new DateTime(2001, 10, 22), row.DateOfBirth);
            Assert.AreEqual(1, row.ContactTypeId);

            row = res[1];
            Assert.AreEqual(2, row.ContactId);
            Assert.AreEqual("Jane", row.Name);
            Assert.AreEqual("1234", row.Phone);
            Assert.AreEqual(null, row.DateOfBirth);
            Assert.AreEqual(2, row.ContactTypeId);

            // Check that the person data was updated as expected - converted and auto-assigned id, plus createdby and createddate columns, and finally runtime variable.
            var res2 = (await db.SqlStatement("SELECT * FROM [Test].[Person]").SelectAsync(dr => new
            {
                PersonId = dr.GetValue<Guid>("PersonId"),
                Name = dr.GetValue<string>("Name"),
                CreatedBy = dr.GetValue<string>("CreatedBy"),
                CreatedDate = dr.GetValue<DateTime>("CreatedDate"),
            }).ConfigureAwait(false)).ToList();

            Assert.AreEqual(2, res.Count);
            var row2 = res2[0];
            Assert.AreEqual(88.ToGuid(), row2.PersonId);
            Assert.AreEqual("RUNTIME", row2.Name);
            Assert.AreEqual(m.ParserArgs.UserName, row2.CreatedBy);
            Assert.AreEqual(m.ParserArgs.DateTimeNow, row2.CreatedDate);

            row2 = res2[1];
            Assert.AreNotEqual(Guid.Empty, row2.PersonId);
            Assert.AreEqual("Bazza", row2.Name);
            Assert.AreEqual(m.ParserArgs.UserName, row2.CreatedBy);
            Assert.AreEqual(m.ParserArgs.DateTimeNow, row2.CreatedDate);

            // Check that the stored procedure script was migrated and works!
            res = (await db.StoredProcedure("[Test].[spGetContact]", p => p.Param("@ContactId", 2)).SelectAsync(dr => new
            {
                ContactId = dr.GetValue<int>("ContactId"),
                Name = dr.GetValue<string>("Name"),
                Phone = dr.GetValue<string>("Phone"),
                DateOfBirth = dr.GetValue<DateTime?>("DateOfBirth"),
                ContactTypeId = dr.GetValue<int>("ContactTypeId")
            }).ConfigureAwait(false)).ToList();

            Assert.AreEqual(1, res.Count);
            row = res[0];
            Assert.AreEqual(2, row.ContactId);
            Assert.AreEqual("Jane", row.Name);
            Assert.AreEqual("1234", row.Phone);
            Assert.AreEqual(null, row.DateOfBirth);
            Assert.AreEqual(2, row.ContactTypeId);
        }
    }
}