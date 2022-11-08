using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CoreEx.Database;
using CoreEx.Database.SqlServer;
using DbEx.Migration.Data;
using DbEx.Migration.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

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
        public async Task A120_MigrateAll_Error()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("ErrorDb");
            var l = UnitTest.GetLogger<SqlServerMigratorTest>();
            var m = new SqlServerMigrator(cs, Migration.MigrationCommand.DropAndAll, l, typeof(Error.TestError).Assembly);
            var r = await m.MigrateAsync().ConfigureAwait(false);

            Assert.IsFalse(r);

            using var db = new SqlServerDatabase(() => new SqlConnection(cs));
            var res = await db.SqlStatement("IF (OBJECT_ID(N'Test.Gender') IS NULL) SELECT 0 ELSE SELECT 1").ScalarAsync<int>().ConfigureAwait(false);
            Assert.AreEqual(0, res, "Test.Gender script should not have been executed as prior should have failed.");
        }

        [Test]
        public async Task A130_MigrateAll_Console()
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
        public async Task A140_Reset_None()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("NoneDb");
            var l = UnitTest.GetLogger<SqlServerMigratorTest>();
            var m = new SqlServerMigrator(cs, Migration.MigrationCommand.Reset, l);
            var r = await m.MigrateAsync().ConfigureAwait(false);

            Assert.IsTrue(r);
        }

        [Test]
        public async Task A150_Reset_Console()
        {
            var (cs, l, m) = await CreateConsoleDb().ConfigureAwait(false);
            using var db = new SqlServerDatabase(() => new SqlConnection(cs));

            // There should be data loaded in Test.Contact.
            var c = await db.SqlStatement("SELECT COUNT(*) FROM Test.Contact").ScalarAsync<int>().ConfigureAwait(false);
            Assert.That(c, Is.GreaterThanOrEqualTo(1));

            // Execute Reset.
            m = new SqlServerMigrator(cs, Migration.MigrationCommand.Reset, l);
            var r = await m.MigrateAsync().ConfigureAwait(false);
            Assert.IsTrue(r);

            // There should now be no data in Test.Contact.
            c = await db.SqlStatement("SELECT COUNT(*) FROM Test.Contact").ScalarAsync<int>().ConfigureAwait(false);
            Assert.That(c, Is.EqualTo(0));

            // Tables in dbo schema should not be touched.
            c = await db.SqlStatement("SELECT COUNT(*) FROM [dbo].[SchemaVersions]").ScalarAsync<int>().ConfigureAwait(false);
            Assert.That(c, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public async Task B100_Execute_Console_Success()
        {
            var c = await CreateConsoleDb().ConfigureAwait(false);
            var m = new SqlServerMigrator(c.cs, Migration.MigrationCommand.Execute, c.l, typeof(Console.Program).Assembly);

            var r = await m.ExecuteSqlStatementsAsync(new string[] { "SELECT * FROM Test.Contact" }).ConfigureAwait(false);
            Assert.IsTrue(r);
        }

        [Test]
        public async Task B110_Execute_Console_Error()
        {
            var c = await CreateConsoleDb().ConfigureAwait(false);
            var m = new SqlServerMigrator(c.cs, Migration.MigrationCommand.Execute, c.l, typeof(Console.Program).Assembly);

            var r = await m.ExecuteSqlStatementsAsync(new string[] { "SELECT * FROM Test.Contact", "SELECT BANANAS" }).ConfigureAwait(false);
            Assert.IsFalse(r);
        }

        [Test]
        public async Task B120_Execute_Console_Batch_Error()
        {
            var c = await CreateConsoleDb().ConfigureAwait(false);
            var m = new SqlServerMigrator(c.cs, Migration.MigrationCommand.Execute, c.l, typeof(Console.Program).Assembly);

            var r = await m.ExecuteSqlStatementsAsync(new string[] { @"SELECT * FROM Test.Contact; /* end */ GO; SELECT * FROM Test.Contact -- comment" }).ConfigureAwait(false);
            Assert.IsFalse(r);
        }

        [Test]
        public async Task B120_Execute_Console_Batch_Success()
        {
            var c = await CreateConsoleDb().ConfigureAwait(false);
            var m = new SqlServerMigrator(c.cs, Migration.MigrationCommand.Execute, c.l, typeof(Console.Program).Assembly);

            var r = await m.ExecuteSqlStatementsAsync(new string[] { @"SELECT * FROM Test.Contact;
/* end */ 
GO 
SELECT * FROM Test.Contact -- comment" }).ConfigureAwait(false);
            Assert.IsTrue(r);
        }

        [Test]
        public void C100_CleanAndSplitSql()
        {
            var stmts = SqlServerMigrator.SplitAndCleanSql(new StringReader("SELECT * FROM Test.Contact; /* end */ GO; SELECT * FROM Test.Contact -- comment"));
            Assert.That(stmts, Is.Not.Null);
            Assert.That(stmts, Has.Count.EqualTo(1));
            Assert.That(stmts[0].OriginalSql, Is.EqualTo(@"SELECT * FROM Test.Contact; /* end */ GO; SELECT * FROM Test.Contact -- comment
"));
            Assert.That(stmts[0].CleanSql, Is.EqualTo(@"SELECT * FROM Test.Contact;  GO; SELECT * FROM Test.Contact 
"));
        }

        [Test]
        public void C110_CleanAndSplitSql()
        {
            var stmts = SqlServerMigrator.SplitAndCleanSql(new StringReader(@"SELECT * FROM Test.Contact;
/* begin
   end */
GO 
SELECT * FROM Test.Contact -- comment"));
            Assert.That(stmts, Is.Not.Null);
            Assert.That(stmts, Has.Count.EqualTo(2));
            Assert.That(stmts[0].OriginalSql, Is.EqualTo(@"SELECT * FROM Test.Contact;
/* begin
   end */
"));
            Assert.That(stmts[0].CleanSql, Is.EqualTo(@"SELECT * FROM Test.Contact;
"));
            Assert.That(stmts[1].OriginalSql, Is.EqualTo(@"SELECT * FROM Test.Contact -- comment
"));
            Assert.That(stmts[1].CleanSql, Is.EqualTo(@"SELECT * FROM Test.Contact 
"));
        }

        [Test]
        public void C120_CleanAndSplitSql()
        {
            var stmts = SqlServerMigrator.SplitAndCleanSql(new StringReader(@"SELECT 'abc\  
def' AS [ColumnResult];"));

            Assert.That(stmts, Is.Not.Null);
            Assert.That(stmts, Has.Count.EqualTo(1));
            Assert.That(stmts[0].OriginalSql, Is.EqualTo(@"SELECT 'abc\  
def' AS [ColumnResult];
"));
            Assert.That(stmts[0].CleanSql, Is.EqualTo(@"SELECT 'abc\  
def' AS [ColumnResult];
"));
        }

        [Test]
        public void C130_CleanAndSplitSql()
        {
            var stmts = SqlServerMigrator.SplitAndCleanSql(new StringReader(@"SELECT * FROM Test.Contact;
GO 
"));
            Assert.AreEqual(2, stmts.Count);
            Assert.That(stmts[0].OriginalSql, Is.EqualTo(@"SELECT * FROM Test.Contact;
"));
            Assert.That(stmts[0].CleanSql, Is.EqualTo(@"SELECT * FROM Test.Contact;
"));
            Assert.That(stmts[1].OriginalSql, Is.EqualTo(@""));
            Assert.That(stmts[1].CleanSql, Is.EqualTo(@""));
        }

        [Test]
        public void C140_CleanAndSplitSql()
        {
            var stmts = SqlServerMigrator.SplitAndCleanSql(new StringReader(@"SELECT /* * FROM */ Test.Contact /* where */ WHERE;
GO 
SELECT COUNT(*) -- comment"));

            Assert.AreEqual(2, stmts.Count);
            Assert.That(stmts[0].OriginalSql, Is.EqualTo(@"SELECT /* * FROM */ Test.Contact /* where */ WHERE;
"));
            Assert.That(stmts[0].CleanSql, Is.EqualTo(@"SELECT  Test.Contact  WHERE;
"));
            Assert.That(stmts[1].OriginalSql, Is.EqualTo(@"SELECT COUNT(*) -- comment
"));
            Assert.That(stmts[1].CleanSql, Is.EqualTo(@"SELECT COUNT(*) 
"));
        }

        [Test]
        public void C150_CleanAndSplitSql()
        {
            var stmts = SqlServerMigrator.SplitAndCleanSql(new StringReader(@"SELECT /* * FROM */ Test.Contact /* where */ WHERE;
GO 
GO
SELECT COUNT(*) -- comment"));

            Assert.AreEqual(3, stmts.Count);
            Assert.That(stmts[0].OriginalSql, Is.EqualTo(@"SELECT /* * FROM */ Test.Contact /* where */ WHERE;
"));
            Assert.That(stmts[0].CleanSql, Is.EqualTo(@"SELECT  Test.Contact  WHERE;
"));
            Assert.That(stmts[1].OriginalSql, Is.EqualTo(@""));
            Assert.That(stmts[1].CleanSql, Is.EqualTo(@""));
            Assert.That(stmts[2].OriginalSql, Is.EqualTo(@"SELECT COUNT(*) -- comment
"));
            Assert.That(stmts[2].CleanSql, Is.EqualTo(@"SELECT COUNT(*) 
"));
        }

        [Test]
        public void C160_CleanAndSplitSql()
        {
            var stmts = SqlServerMigrator.SplitAndCleanSql(new StringReader(@"SELECT /* * FROM */ Test.Contact /* where */ WHERE;
GO 
-- Bananas
GO
SELECT COUNT(*) -- comment"));

            Assert.AreEqual(3, stmts.Count);
            Assert.That(stmts[0].OriginalSql, Is.EqualTo(@"SELECT /* * FROM */ Test.Contact /* where */ WHERE;
"));
            Assert.That(stmts[0].CleanSql, Is.EqualTo(@"SELECT  Test.Contact  WHERE;
"));
            Assert.That(stmts[1].OriginalSql, Is.EqualTo(@"-- Bananas
"));
            Assert.That(stmts[1].CleanSql, Is.EqualTo(@""));
            Assert.That(stmts[2].OriginalSql, Is.EqualTo(@"SELECT COUNT(*) -- comment
"));
            Assert.That(stmts[2].CleanSql, Is.EqualTo(@"SELECT COUNT(*) 
"));
        }

        [Test]
        public void C170_CleanAndSplitSql()
        {
            var stmts = SqlServerMigrator.SplitAndCleanSql(new StringReader(@"SELECT /* * FROM */ Test.Contact /* where */ WHERE;
GO 
/* */
GO
SELECT COUNT(*) -- comment"));

            Assert.AreEqual(3, stmts.Count);
            Assert.That(stmts[0].OriginalSql, Is.EqualTo(@"SELECT /* * FROM */ Test.Contact /* where */ WHERE;
"));
            Assert.That(stmts[0].CleanSql, Is.EqualTo(@"SELECT  Test.Contact  WHERE;
"));
            Assert.That(stmts[1].OriginalSql, Is.EqualTo(@"/* */
"));
            Assert.That(stmts[1].CleanSql, Is.EqualTo(@""));
            Assert.That(stmts[2].OriginalSql, Is.EqualTo(@"SELECT COUNT(*) -- comment
"));
            Assert.That(stmts[2].CleanSql, Is.EqualTo(@"SELECT COUNT(*) 
"));
        }
    }
}