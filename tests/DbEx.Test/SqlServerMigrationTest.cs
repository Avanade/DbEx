using DbEx.Migration;
using DbEx.Migration.Data;
using DbEx.SqlServer.Console;
using DbEx.SqlServer.Migration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace DbEx.Test
{
    [TestFixture]
    [NonParallelizable]
    public class SqlServerMigrationTest
    {
        [Test]
        public async Task A100_MigrateAll_None()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("NoneDb");
            var l = UnitTest.GetLogger<SqlServerMigrationTest>();
            var a = new MigrationArgs(MigrationCommand.DropAndAll, cs) { Logger = l };
            using var m = new SqlServerMigration(a);
            var r = await m.MigrateAsync().ConfigureAwait(false);

            Assert.IsTrue(r);
        }

        [Test]
        public async Task A110_MigrateAll_Empty()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("EmptyDb");
            var l = UnitTest.GetLogger<SqlServerMigrationTest>();
            var a = new MigrationArgs(MigrationCommand.DropAndAll, cs) { Logger = l }.AddAssembly(typeof(Empty.Test));
            using var m = new SqlServerMigration(a);
            var r = await m.MigrateAsync().ConfigureAwait(false);

            Assert.IsTrue(r);
        }

        [Test]
        public async Task A120_MigrateAll_Error()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("ErrorDb");
            var l = UnitTest.GetLogger<SqlServerMigrationTest>();
            var a = new MigrationArgs(MigrationCommand.DropAndAll, cs) { Logger = l }.AddAssembly(typeof(Error.TestError));
            using var m = new SqlServerMigration(a);
            var r = await m.MigrateAsync().ConfigureAwait(false);

            Assert.IsFalse(r);

            using var db = new SqlServerDatabase(() => new SqlConnection(cs));
            var res = await db.SqlStatement("IF (OBJECT_ID(N'Test.Gender') IS NULL) SELECT 0 ELSE SELECT 1").ScalarAsync<int>().ConfigureAwait(false);
            Assert.AreEqual(0, res, "Test.Gender script should not have been executed as prior should have failed.");
        }

        [Test]
        public async Task A120_MigrateAll_With_Log_Output()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("ErrorDb");
            var l = UnitTest.GetLogger<SqlServerMigrationTest>();
            var a = new MigrationArgs(MigrationCommand.DropAndAll, cs) { Logger = l }.AddAssembly(typeof(Error.TestError));
            using var m = new SqlServerMigration(a);
            var (Success, Output) = await m.MigrateAndLogAsync().ConfigureAwait(false);

            Assert.IsFalse(Success);
            Assert.IsTrue(Output.Length > 0);
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
                DateOfBirth = dr.GetValue<DateOnly?>("DateOfBirth"),
                ContactTypeId = dr.GetValue<int>("ContactTypeId"),
                GenderId = dr.GetValue<int?>("GenderId"),
                TenantId = dr.GetValue<string>("TenantId")
            }).ConfigureAwait(false)).ToList();

            Assert.AreEqual(3, res.Count);

            var row = res[0];
            Assert.AreEqual(1, row.ContactId);
            Assert.AreEqual("Bob", row.Name);
            Assert.AreEqual(null, row.Phone);
            Assert.AreEqual(new DateOnly(2001, 10, 22), row.DateOfBirth);
            Assert.AreEqual(1, row.ContactTypeId);
            Assert.AreEqual(2, row.GenderId);
            Assert.AreEqual("test-tenant", row.TenantId);

            row = res[1];
            Assert.AreEqual(2, row.ContactId);
            Assert.AreEqual("Jane", row.Name);
            Assert.AreEqual("1234", row.Phone);
            Assert.AreEqual(null, row.DateOfBirth);
            Assert.AreEqual(2, row.ContactTypeId);
            Assert.IsNull(row.GenderId);
            Assert.AreEqual("test-tenant", row.TenantId);

            row = res[2];
            Assert.AreEqual(3, row.ContactId);
            Assert.AreEqual("Barry", row.Name);
            Assert.AreEqual(null, row.Phone);
            Assert.AreEqual(new DateOnly(2001, 10, 22), row.DateOfBirth);
            Assert.AreEqual(1, row.ContactTypeId);
            Assert.AreEqual(2, row.GenderId);
            Assert.IsNull(row.TenantId); // Must be set within SQL script itself; the column default does not extend to SQL scripts themselves.

            // Check that the person data was updated as expected - converted and auto-assigned id, plus createdby and createddate columns, and finally runtime variable.
            var res2 = (await db.SqlStatement("SELECT * FROM [Test].[Person]").SelectQueryAsync(dr => new
            {
                PersonId = dr.GetValue<Guid>("PersonId"),
                Name = dr.GetValue<string>("Name"),
                CreatedBy = dr.GetValue<string>("CreatedBy"),
                CreatedOn = dr.GetValue<DateTimeOffset>("CreatedOn"),
                AddressJson = dr.GetValue<string>("AddressJson"),
                NicknamesJson = dr.GetValue<string>("NicknamesJson")
            }).ConfigureAwait(false)).ToList();

            Assert.AreEqual(3, res.Count);
            var row2 = res2[0];
            Assert.AreEqual(DataValueConverter.IntToGuid(88), row2.PersonId);
            Assert.AreEqual("RUNTIME", row2.Name);
            Assert.AreEqual(m.Args.DataParserArgs.UserName, row2.CreatedBy);
            Assert.AreEqual(m.Args.DataParserArgs.DateTimeNow, row2.CreatedOn);

            row2 = res2[1];
            Assert.AreNotEqual(Guid.Empty, row2.PersonId);
            Assert.AreEqual("Bazza", row2.Name);
            Assert.AreEqual(m.Args.DataParserArgs.UserName, row2.CreatedBy);
            Assert.AreEqual(m.Args.DataParserArgs.DateTimeNow, row2.CreatedOn);
            Assert.AreEqual("{\"Street\": \"Main St\", \"City\": \"Maine\"}", row2.AddressJson);
            Assert.AreEqual("[\"Gaz\", \"Baz\"]", row2.NicknamesJson);

            // Check that the stored procedure script was migrated and works!
            res = [.. (await db.StoredProcedure("[Test].[spGetContact]").Param("@ContactId", 2).SelectQueryAsync(dr => new
            {
                ContactId = dr.GetValue<int>("ContactId"),
                Name = dr.GetValue<string>("Name"),
                Phone = dr.GetValue<string>("Phone"),
                DateOfBirth = dr.GetValue<DateOnly?>("DateOfBirth"),
                ContactTypeId = dr.GetValue<int>("ContactTypeId"),
                GenderId = dr.GetValue<int?>("GenderId"),
                TenantId = dr.GetValue<string>("TenantId")
            }).ConfigureAwait(false))];

            Assert.AreEqual(1, res.Count);
            row = res[0];
            Assert.AreEqual(2, row.ContactId);
            Assert.AreEqual("Jane", row.Name);
            Assert.AreEqual("1234", row.Phone);
            Assert.AreEqual(null, row.DateOfBirth);
            Assert.AreEqual(2, row.ContactTypeId);
            Assert.IsNull(row.GenderId);
        }

        private static async Task<(string cs, ILogger l, SqlServerMigration m)> CreateConsoleDb()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("ConsoleDb");
            var l = UnitTest.GetLogger<SqlServerMigrationTest>();
            var a = new MigrationArgs(MigrationCommand.DropAndAll, cs) { Logger = l }.AddAssembly(typeof(Console.Program)).IncludeExtendedSchemaScripts();
            using var m = new SqlServerMigration(a);

            m.Args.DataParserArgs.Parameters.Add("DefaultName", "Bazza");
            m.Args.DataParserArgs.Parameters.Add("jane_name", "Jane");
            m.Args.DataParserArgs.RefDataColumnDefaults.Add("SortOrder", i => i);
            m.Args.DataParserArgs.ColumnDefaults.Add(new DataParserColumnDefault("*", "*", "TenantId", _ => "test-tenant"));
            m.Args.DataParserArgs.TableNameMappings.Add("XTest", "XContactType", "Test", "ContactType", new() { { "XNumber", "Number" } })
                                                   .Add("Test", "Addresses", "Test", "ContactAddress");

            var r = await m.MigrateAsync().ConfigureAwait(false);

            Assert.IsTrue(r);

            return (cs, l, m);
        }

        [Test]
        public async Task C110_Throw_Exceptions()
        {
            var (cs, l, m) = await CreateConsoleDb().ConfigureAwait(false);
            using var db = new SqlServerDatabase(() => new SqlConnection(cs));

            Assert.AreEqual(Assert.ThrowsAsync<SqlException>(() => db.StoredProcedure("spThrowAuthorizationException").Param("message", (string)null).NonQueryAsync()).Number, 56003);
            Assert.AreEqual(Assert.ThrowsAsync<SqlException>(() => db.StoredProcedure("spThrowBusinessException").Param("message", (string)null).NonQueryAsync()).Number, 56002);
            Assert.AreEqual(Assert.ThrowsAsync<SqlException>(() => db.StoredProcedure("spThrowConcurrencyException").Param("message", (string)null).NonQueryAsync()).Number, 56004);
            Assert.AreEqual(Assert.ThrowsAsync<SqlException>(() => db.StoredProcedure("spThrowConflictException").Param("message", (string)null).NonQueryAsync()).Number, 56006);
            Assert.AreEqual(Assert.ThrowsAsync<SqlException>(() => db.StoredProcedure("spThrowDuplicateException").Param("message", (string)null).NonQueryAsync()).Number, 56007);
            Assert.AreEqual(Assert.ThrowsAsync<SqlException>(() => db.StoredProcedure("spThrowNotFoundException").Param("message", (string)null).NonQueryAsync()).Number, 56005);
            Assert.AreEqual(Assert.ThrowsAsync<SqlException>(() => db.StoredProcedure("spThrowValidationException").Param("message", (string)null).NonQueryAsync()).Number, 56001);

            var vex = Assert.ThrowsAsync<SqlException>(() => db.StoredProcedure("spThrowValidationException").Param("message", "On no!").NonQueryAsync());
            Assert.AreEqual("On no!", vex.Message);
        }

        [Test]
        public async Task C120_Set_Session_Context()
        {
            var (cs, l, m) = await CreateConsoleDb().ConfigureAwait(false);
            using var db = new SqlServerDatabase(() => new SqlConnection(cs));

            var now = DateTimeOffset.UtcNow;
            var ts = new DateTimeOffset(2024, 09, 30, 23, 45, 08, 123, TimeSpan.FromHours(8));
            var tsUtc = ts.ToUniversalTime();

            await db.StoredProcedure("[dbo].[spSetSessionContext]")
                .Param("@Username", "bob@gmail.com")
                .Param("@Timestamp", ts)
                .Param("@TenantId", "banana")
                .Param("@UserId", "bob2")
                .NonQueryAsync().ConfigureAwait(false);

            Assert.That(await db.SqlStatement("select dbo.fnGetTimestamp(null)").ScalarAsync<DateTimeOffset>(), Is.EqualTo(tsUtc));
            Assert.That(await db.SqlStatement("select dbo.fnGetUsername(null)").ScalarAsync<string>(), Is.EqualTo("bob@gmail.com"));
            Assert.That(await db.SqlStatement("select dbo.fnGetTenantId(null)").ScalarAsync<string>(), Is.EqualTo("banana"));
            Assert.That(await db.SqlStatement("select dbo.fnGetUserId(null)").ScalarAsync<string>(), Is.EqualTo("bob2"));

            // Make sure the session context doesn't leak between connections.
            using var db2 = new SqlServerDatabase(() => new SqlConnection(cs));
            Assert.That(await db2.SqlStatement("select dbo.fnGetTimestamp(null)").ScalarAsync<DateTimeOffset>(), Is.GreaterThanOrEqualTo(now));
            Assert.That(await db2.SqlStatement("select dbo.fnGetUsername(null)").ScalarAsync<string>(), Is.Not.Null.And.Not.EqualTo("bob@gmail.com"));
            Assert.That(await db2.SqlStatement("select dbo.fnGetTenantId(null)").ScalarAsync<string>(), Is.Null);
            Assert.That(await db2.SqlStatement("select dbo.fnGetUserId(null)").ScalarAsync<string>(), Is.Null);
        }

        [Test]
        public async Task A140_Reset_None()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("NoneDb");
            var l = UnitTest.GetLogger<SqlServerMigrationTest>();
            var a = new MigrationArgs(MigrationCommand.Reset, cs) { Logger = l };
            using var m = new SqlServerMigration(a);
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
            var a = new MigrationArgs(MigrationCommand.Reset, cs) { Logger = l };
            m = new SqlServerMigration(a);
            var r = await m.MigrateAsync().ConfigureAwait(false);
            Assert.IsTrue(r);

            // There should now be no data in Test.Contact.
            c = await db.SqlStatement("SELECT COUNT(*) FROM Test.Contact").ScalarAsync<int>().ConfigureAwait(false);
            Assert.That(c, Is.EqualTo(0));

            // Tables in dbo schema should not be touched.
            c = await db.SqlStatement("SELECT COUNT(*) FROM [dbo].[SchemaVersions]").ScalarAsync<int>().ConfigureAwait(false);
            Assert.That(c, Is.GreaterThanOrEqualTo(1));

            m.Dispose();
        }

        [Test]
        public async Task B100_Execute_Console_Success()
        {
            var c = await CreateConsoleDb().ConfigureAwait(false);
            var a = new MigrationArgs(MigrationCommand.Execute, c.cs) { Logger = c.l }.AddAssembly(typeof(Console.Program).Assembly);
            using var m = new SqlServerMigration(a);

            var r = await m.ExecuteSqlStatementsAsync(["SELECT * FROM Test.Contact"]).ConfigureAwait(false);
            Assert.IsTrue(r);
        }

        [Test]
        public async Task B110_Execute_Console_Error()
        {
            var c = await CreateConsoleDb().ConfigureAwait(false);
            var a = new MigrationArgs(MigrationCommand.Execute, c.cs) { Logger = c.l }.AddAssembly(typeof(Console.Program).Assembly);
            using var m = new SqlServerMigration(a);

            var r = await m.ExecuteSqlStatementsAsync(["SELECT * FROM Test.Contact", "SELECT BANANAS"]).ConfigureAwait(false);
            Assert.IsFalse(r);
        }

        [Test]
        public async Task B120_Execute_Console_Batch_Error()
        {
            var c = await CreateConsoleDb().ConfigureAwait(false);
            var a = new MigrationArgs(MigrationCommand.Execute, c.cs) { Logger = c.l }.AddAssembly(typeof(Console.Program).Assembly);
            using var m = new SqlServerMigration(a);

            var r = await m.ExecuteSqlStatementsAsync([@"SELECT * FROM Test.ContactBad; /* end */ GO; SELECT * FROM Test.Contact -- comment"]).ConfigureAwait(false);
            Assert.IsFalse(r);
        }

        [Test]
        public async Task B130_Execute_Console_Batch_Success()
        {
            var c = await CreateConsoleDb().ConfigureAwait(false);
            var a = new MigrationArgs(MigrationCommand.Execute, c.cs) { Logger = c.l }.AddAssembly(typeof(Console.Program).Assembly);
            using var m = new SqlServerMigration(a);

            var r = await m.ExecuteSqlStatementsAsync([ @"SELECT * FROM Test.Contact;
/* end */ 
GO 
SELECT * FROM Test.Contact -- comment" ]).ConfigureAwait(false);
            Assert.IsTrue(r);
        }

        [Test]
        public async Task SqlServerSchemaScript_SchemaAndObject()
        {
            var c = await CreateConsoleDb().ConfigureAwait(false);
            var a = new MigrationArgs(MigrationCommand.Execute, c.cs) { Logger = c.l }.AddAssembly(typeof(Console.Program).Assembly);
            using var m = new SqlServerMigration(a);

            var ss = SqlServerSchemaScript.Create(new Migration.DatabaseMigrationScript(m, "CREATE PROC [Ref].[USStates] ( )", "blah"));
            Assert.That(ss.HasError, Is.False);
            Assert.That(ss.Schema, Is.EqualTo("Ref"));
            Assert.That(ss.Name, Is.EqualTo("USStates"));
            Assert.That(ss.SupportsReplace, Is.False);
        }

        [Test]
        public async Task SqlServerSchemaScript_SchemaAndObjectWithAlter()
        {
            var c = await CreateConsoleDb().ConfigureAwait(false);
            var a = new MigrationArgs(MigrationCommand.Execute, c.cs) { Logger = c.l }.AddAssembly(typeof(Console.Program).Assembly);
            using var m = new SqlServerMigration(a);

            var ss = SqlServerSchemaScript.Create(new Migration.DatabaseMigrationScript(m, "CREATE OR ALTER PROC [Ref].[USStates] ( )", "blah"));
            Assert.That(ss.HasError, Is.False);
            Assert.That(ss.Schema, Is.EqualTo("Ref"));
            Assert.That(ss.Name, Is.EqualTo("USStates"));
            Assert.That(ss.SupportsReplace, Is.True);
        }

        [Test]
        public async Task SqlServerSchemaScript_NoSchemaAndObject()
        {
            var c = await CreateConsoleDb().ConfigureAwait(false);
            var a = new MigrationArgs(MigrationCommand.Execute, c.cs) { Logger = c.l }.AddAssembly(typeof(Console.Program).Assembly);
            using var m = new SqlServerMigration(a);

            var ss = SqlServerSchemaScript.Create(new Migration.DatabaseMigrationScript(m, "CREATE PROC [USStates] ( )", "blah"));
            Assert.That(ss.HasError, Is.False);
            Assert.That(ss.Schema, Is.EqualTo("dbo"));
            Assert.That(ss.Name, Is.EqualTo("USStates"));
        }

        [Test]
        public async Task SqlServerSchemaScript_FunctionWithBrackets()
        {
            var c = await CreateConsoleDb().ConfigureAwait(false);
            var a = new MigrationArgs(MigrationCommand.Execute, c.cs) { Logger = c.l }.AddAssembly(typeof(Console.Program).Assembly);
            using var m = new SqlServerMigration(a);

            var ss = SqlServerSchemaScript.Create(new Migration.DatabaseMigrationScript(m, "CREATE FUNCTION [Sec].[fnGetUserHasPermission]( some, other='stuf', num = 1.3 );", "blah"));
            Assert.That(ss.HasError, Is.False);
            Assert.That(ss.Schema, Is.EqualTo("Sec"));
            Assert.That(ss.Name, Is.EqualTo("fnGetUserHasPermission"));
        }

        [Test]
        public async Task SqlServerSchemaScript_FunctionWithBrackets2()
        {
            var c = await CreateConsoleDb().ConfigureAwait(false);
            var a = new MigrationArgs(MigrationCommand.Execute, c.cs) { Logger = c.l }.AddAssembly(typeof(Console.Program).Assembly);
            using var m = new SqlServerMigration(a);

            var ss = SqlServerSchemaScript.Create(new Migration.DatabaseMigrationScript(m, @"CREATE FUNCTION [Sec].[fnGetUserHasPermission]()
some other stuf", "blah"));

            Assert.That(ss.HasError, Is.False);
            Assert.That(ss.Schema, Is.EqualTo("Sec"));
            Assert.That(ss.Name, Is.EqualTo("fnGetUserHasPermission"));
        }
    }
}