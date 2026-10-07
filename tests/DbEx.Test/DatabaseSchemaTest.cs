using DbEx.Migration;
using DbEx.DbSchema;
using DbEx.MySql.Migration;
using DbEx.Postgres.Migration;
using DbEx.SqlServer.Migration;
using DbEx.Test.PostgresConsole;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace DbEx.Test
{
    [TestFixture]
    [NonParallelizable]
    public class DatabaseSchemaTest
    {
        [Test]
        public async Task SqlServerSelectSchema()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("ConsoleDb");
            var l = UnitTest.GetLogger<DatabaseSchemaTest>();
            var a = new MigrationArgs(MigrationCommand.Drop | MigrationCommand.Create | MigrationCommand.Migrate | MigrationCommand.Schema, cs) { Logger = l }.AddAssembly(typeof(Console.Program));
            using var m = new SqlServerMigration(a);
            var r = await m.MigrateAsync().ConfigureAwait(false);
            Assert.IsTrue(r);

            using var db = new SqlServerDatabase(() => new SqlConnection(cs));
            var tables = await db.SelectSchemaAsync(m).ConfigureAwait(false);
            Assert.IsNotNull(tables);

            // [Test].[ContactType]
            var tab = tables.Where(x => x.Name == "ContactType").SingleOrDefault();
            Assert.IsNotNull(tab);
            Assert.AreEqual("Test", tab.Schema);
            Assert.AreEqual("ContactType", tab.Name);
            Assert.AreEqual("ct", tab.Alias);
            Assert.AreEqual("[Test].[ContactType]", tab.QualifiedName);
            Assert.IsFalse(tab.IsAView);
            Assert.IsTrue(tab.IsRefData);
            Assert.AreEqual(6, tab.Columns.Count);
            Assert.AreEqual(1, tab.PrimaryKeyColumns.Count);
            Assert.AreEqual("ContactType", tab.DotNetName);
            Assert.AreEqual("ContactTypes", tab.PluralName);

            var col = tab.Columns[0];
            Assert.AreEqual("ContactTypeId", col.Name);
            Assert.AreEqual("int", col.Type);
            Assert.AreEqual("INT", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.AreEqual(0, col.Scale);
            Assert.AreEqual(10, col.Precision);
            Assert.AreEqual("int", col.DotNetType);
            Assert.IsFalse(col.IsNullable);
            Assert.IsTrue(col.IsPrimaryKey);
            Assert.IsTrue(col.IsIdentity);
            Assert.AreEqual(1, col.IdentitySeed);
            Assert.AreEqual(1, col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);

            col = tab.Columns[1];
            Assert.AreEqual("Code", col.Name);
            Assert.AreEqual("nvarchar", col.Type);
            Assert.AreEqual("NVARCHAR(50)", col.SqlType);
            Assert.AreEqual(50, col.Length);
            Assert.IsNull(col.Scale);
            Assert.IsNull(col.Precision);
            Assert.AreEqual("string", col.DotNetType);
            Assert.IsFalse(col.IsNullable);
            Assert.IsFalse(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsTrue(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);

            col = tab.Columns[2];
            Assert.AreEqual("Text", col.Name);

            col = tab.Columns[3];
            Assert.AreEqual("SortOrder", col.Name);

            // [Test].[Contact]
            tab = tables.Where(x => x.Name == "Contact").SingleOrDefault();
            Assert.IsNotNull(tab);
            Assert.AreEqual("Test", tab.Schema);
            Assert.AreEqual("Contact", tab.Name);
            Assert.AreEqual("c", tab.Alias);
            Assert.AreEqual("[Test].[Contact]", tab.QualifiedName);
            Assert.IsFalse(tab.IsAView);
            Assert.IsFalse(tab.IsRefData);
            Assert.AreEqual(9, tab.Columns.Count);
            Assert.AreEqual(1, tab.PrimaryKeyColumns.Count);
            Assert.AreEqual("Contact", tab.DotNetName);
            Assert.AreEqual("Contacts", tab.PluralName);

            col = tab.Columns[0];
            Assert.AreEqual("ContactId", col.Name);
            Assert.AreEqual("int", col.Type);
            Assert.AreEqual("INT", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.AreEqual(0, col.Scale);
            Assert.AreEqual(10, col.Precision);
            Assert.AreEqual("int", col.DotNetType);
            Assert.IsFalse(col.IsNullable);
            Assert.IsTrue(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);

            col = tab.Columns[3];
            Assert.AreEqual("DateOfBirth", col.Name);
            Assert.AreEqual("date", col.Type);
            Assert.AreEqual("DATE NULL", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.IsNull(col.Scale);
            Assert.AreEqual(0, col.Precision);
            Assert.AreEqual("DateOnly", col.DotNetType);
            Assert.IsTrue(col.IsNullable);
            Assert.IsFalse(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);

            col = tab.Columns[4];
            Assert.AreEqual("ContactTypeId", col.Name);
            Assert.AreEqual("int", col.Type);
            Assert.AreEqual("INT", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.AreEqual(0, col.Scale);
            Assert.AreEqual(10, col.Precision);
            Assert.AreEqual("int", col.DotNetType);
            Assert.IsFalse(col.IsNullable);
            Assert.IsFalse(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsTrue(col.IsForeignRefData);
            Assert.AreEqual("Test", col.ForeignSchema);
            Assert.AreEqual("ContactType", col.ForeignTable);
            Assert.AreEqual("ContactTypeId", col.ForeignColumn);
            Assert.AreEqual("Code", col.ForeignRefDataCodeColumn);
            Assert.AreEqual("((1))", col.DefaultValue);
            Assert.AreEqual("ContactTypeId", col.DotNetName);
            Assert.AreEqual("ContactType", col.DotNetCleanedName);
            Assert.IsTrue(col.IsRefData);

            col = tab.Columns[5];
            Assert.AreEqual("GenderId", col.Name);
            Assert.AreEqual("int", col.Type);
            Assert.AreEqual("INT NULL", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.AreEqual(0, col.Scale);
            Assert.AreEqual(10, col.Precision);
            Assert.AreEqual("int", col.DotNetType);
            Assert.IsTrue(col.IsNullable);
            Assert.IsFalse(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsTrue(col.IsForeignRefData);
            Assert.AreEqual("Test", col.ForeignSchema);
            Assert.AreEqual("Gender", col.ForeignTable);
            Assert.AreEqual("GenderId", col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);
            Assert.IsFalse(col.IsTenantId);

            col = tab.Columns[6];
            Assert.AreEqual("TenantId", col.Name);
            Assert.IsTrue(col.IsTenantId);

            col = tab.Columns[7];
            Assert.AreEqual("Notes", col.Name);
            Assert.AreEqual("nvarchar", col.Type);
            Assert.AreEqual("NVARCHAR(MAX) NULL", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.IsNull(col.Scale);
            Assert.IsNull(col.Precision);
            Assert.AreEqual("string", col.DotNetType);
            Assert.IsTrue(col.IsNullable);
            Assert.IsFalse(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.DefaultValue);

            col = tab.Columns[8];
            Assert.AreEqual("ContactTypeCode", col.Name);
            Assert.IsTrue(col.IsRefData);

            // [Test].[MultiPk]
            tab = tables.Where(x => x.Name == "MultiPk").SingleOrDefault();
            Assert.IsNotNull(tab);
            Assert.AreEqual("Test", tab.Schema);
            Assert.AreEqual("MultiPk", tab.Name);
            Assert.AreEqual("mp", tab.Alias);
            Assert.AreEqual("[Test].[MultiPk]", tab.QualifiedName);
            Assert.IsFalse(tab.IsAView);
            Assert.IsFalse(tab.IsRefData);
            Assert.AreEqual(4, tab.Columns.Count);
            Assert.AreEqual(2, tab.PrimaryKeyColumns.Count);
            Assert.AreEqual("MultiPk", tab.DotNetName);
            Assert.AreEqual("MultiPks", tab.PluralName);

            col = tab.Columns[0];
            Assert.AreEqual("Part1", col.Name);
            Assert.AreEqual("int", col.Type);
            Assert.AreEqual("INT", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.AreEqual(0, col.Scale);
            Assert.AreEqual(10, col.Precision);
            Assert.AreEqual("int", col.DotNetType);
            Assert.IsFalse(col.IsNullable);
            Assert.IsTrue(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);

            col = tab.Columns[1];
            Assert.AreEqual("Part2", col.Name);
            Assert.AreEqual("int", col.Type);
            Assert.AreEqual("INT", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.AreEqual(0, col.Scale);
            Assert.AreEqual(10, col.Precision);
            Assert.AreEqual("int", col.DotNetType);
            Assert.IsFalse(col.IsNullable);
            Assert.IsTrue(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);

            col = tab.Columns[2];
            Assert.AreEqual("Value", col.Name);
            Assert.AreEqual("decimal", col.Type);
            Assert.AreEqual("DECIMAL(16, 4) NULL", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.AreEqual(4, col.Scale);
            Assert.AreEqual(16, col.Precision);
            Assert.AreEqual("decimal", col.DotNetType);
            Assert.IsTrue(col.IsNullable);
            Assert.IsFalse(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);

            col = tab.Columns[3];
            Assert.AreEqual("Parts", col.Name);
            Assert.AreEqual("nvarchar", col.Type);
            Assert.AreEqual("NVARCHAR(65) NULL", col.SqlType);
            Assert.AreEqual(65, col.Length);
            Assert.IsNull(col.Scale);
            Assert.IsNull(col.Precision);
            Assert.AreEqual("string", col.DotNetType);
            Assert.IsTrue(col.IsNullable);
            Assert.IsFalse(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsTrue(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);

            // [Test].[Person]
            tab = tables.Where(x => x.Name == "Person").SingleOrDefault();
            Assert.IsNotNull(tab);
            Assert.AreEqual("Test", tab.Schema);
            Assert.AreEqual("Person", tab.Name);
            Assert.AreEqual("p", tab.Alias);
            Assert.AreEqual("[Test].[Person]", tab.QualifiedName);
            Assert.IsFalse(tab.IsAView);
            Assert.IsFalse(tab.IsRefData);
            Assert.AreEqual(8, tab.Columns.Count);
            Assert.AreEqual(1, tab.PrimaryKeyColumns.Count);
            Assert.AreEqual("Person", tab.DotNetName);
            Assert.AreEqual("People", tab.PluralName);

            col = tab.Columns[0];
            Assert.AreEqual("PersonId", col.Name);
            Assert.AreEqual("uniqueidentifier", col.Type);
            Assert.AreEqual("UNIQUEIDENTIFIER", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.IsNull(col.Scale);
            Assert.IsNull(col.Precision);
            Assert.AreEqual("Guid", col.DotNetType);
            Assert.IsFalse(col.IsNullable);
            Assert.IsTrue(col.IsPrimaryKey);
            Assert.IsTrue(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNotNull(col.DefaultValue);

            // [Test].[ExtraTypes] - the .NET (EF) type is the "true" type; text is used for data parsing.
            AssertExtraTypes(tables, "ExtraTypes",
            [
                ("Location", "geography", "Geometry", true, "string"),
                ("Shape", "geometry", "Geometry", true, "string"),
                ("Path", "hierarchyid", "HierarchyId", true, "string"),
                ("Document", "xml", "string", true, "string"),
                ("Embedding", "vector", "SqlVector<float>", false, "string"),
                ("Variant", "sql_variant", "object", true, "string"),
                ("Payload", "json", "string", true, "string"),
            ]);

            Assert.AreEqual("VECTOR(3) NULL", tables.Single(x => x.Name == "ExtraTypes").Columns.Single(x => x.Name == "Embedding").SqlType);
        }

        [Test]
        public async Task MySqlSelectSchema()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("MySqlDb");
            var l = UnitTest.GetLogger<DatabaseSchemaTest>();
            var a = new MigrationArgs(MigrationCommand.Drop | MigrationCommand.Create | MigrationCommand.Migrate | MigrationCommand.Schema, cs) { Logger = l }.AddAssembly(typeof(MySqlStuff));
            using var m = new MySqlMigration(a);
            var r = await m.MigrateAsync().ConfigureAwait(false);
            Assert.IsTrue(r);

            using var db = new MySqlDatabase(() => new MySqlConnection(cs));
            var tables = await db.SelectSchemaAsync(m).ConfigureAwait(false);
            Assert.IsNotNull(tables);

            // [Test].[ContactType]
            var tab = tables.Where(x => x.Name == "contact_type").SingleOrDefault();
            Assert.IsNotNull(tab);
            Assert.AreEqual(string.Empty, tab.Schema);
            Assert.AreEqual("contact_type", tab.Name);
            Assert.AreEqual("ct", tab.Alias);
            Assert.AreEqual("`contact_type`", tab.QualifiedName);
            Assert.IsFalse(tab.IsAView);
            Assert.IsTrue(tab.IsRefData);
            Assert.AreEqual(4, tab.Columns.Count);
            Assert.AreEqual(1, tab.PrimaryKeyColumns.Count);
            Assert.AreEqual("ContactType", tab.DotNetName);
            Assert.AreEqual("ContactTypes", tab.PluralName);

            var col = tab.Columns[0];
            Assert.AreEqual("contact_type_id", col.Name);
            Assert.AreEqual("int", col.Type);
            Assert.AreEqual("INT", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.AreEqual(0, col.Scale);
            Assert.AreEqual(10, col.Precision);
            Assert.AreEqual("int", col.DotNetType);
            Assert.IsFalse(col.IsNullable);
            Assert.IsTrue(col.IsPrimaryKey);
            Assert.IsTrue(col.IsIdentity);
            Assert.AreEqual(1, col.IdentitySeed);
            Assert.AreEqual(1, col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);
            Assert.AreEqual("ContactTypeId", col.DotNetName);
            Assert.AreEqual("ContactTypeId", col.DotNetCleanedName);

            col = tab.Columns[1];
            Assert.AreEqual("code", col.Name);
            Assert.AreEqual("varchar", col.Type);
            Assert.AreEqual("VARCHAR(50)", col.SqlType);
            Assert.AreEqual(50, col.Length);
            Assert.IsNull(col.Scale);
            Assert.IsNull(col.Precision);
            Assert.AreEqual("string", col.DotNetType);
            Assert.IsFalse(col.IsNullable);
            Assert.IsFalse(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsTrue(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);
            Assert.AreEqual("Code", col.DotNetName);

            col = tab.Columns[2];
            Assert.AreEqual("text", col.Name);

            col = tab.Columns[3];
            Assert.AreEqual("sort_order", col.Name);

            // [Test].[Contact]
            tab = tables.Where(x => x.Name == "contact").SingleOrDefault();
            Assert.IsNotNull(tab);
            Assert.AreEqual(string.Empty, tab.Schema);
            Assert.AreEqual("contact", tab.Name);
            Assert.AreEqual("c", tab.Alias);
            Assert.AreEqual("`contact`", tab.QualifiedName);
            Assert.IsFalse(tab.IsAView);
            Assert.IsFalse(tab.IsRefData);
            Assert.AreEqual(12, tab.Columns.Count);
            Assert.AreEqual(1, tab.PrimaryKeyColumns.Count);
            Assert.AreEqual("Contact", tab.DotNetName);
            Assert.AreEqual("Contacts", tab.PluralName);

            col = tab.Columns[0];
            Assert.AreEqual("contact_id", col.Name);
            Assert.AreEqual("int", col.Type);
            Assert.AreEqual("INT", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.AreEqual(0, col.Scale);
            Assert.AreEqual(10, col.Precision);
            Assert.AreEqual("int", col.DotNetType);
            Assert.IsFalse(col.IsNullable);
            Assert.IsTrue(col.IsPrimaryKey);
            Assert.IsTrue(col.IsIdentity);
            Assert.AreEqual(1, col.IdentitySeed);
            Assert.AreEqual(1, col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);
            Assert.AreEqual("ContactId", col.DotNetName);

            col = tab.Columns[3];
            Assert.AreEqual("date_of_birth", col.Name);
            Assert.AreEqual("date", col.Type);
            Assert.AreEqual("DATE NULL", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.IsNull(col.Scale);
            Assert.IsNull(col.Precision);
            Assert.AreEqual("DateOnly", col.DotNetType);
            Assert.IsTrue(col.IsNullable);
            Assert.IsFalse(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);
            Assert.AreEqual("DateOfBirth", col.DotNetName);

            col = tab.Columns[4];
            Assert.AreEqual("contact_type_id", col.Name);
            Assert.AreEqual("int", col.Type);
            Assert.AreEqual("INT", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.AreEqual(0, col.Scale);
            Assert.AreEqual(10, col.Precision);
            Assert.AreEqual("int", col.DotNetType);
            Assert.IsFalse(col.IsNullable);
            Assert.IsFalse(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsTrue(col.IsForeignRefData);
            Assert.AreEqual(string.Empty, col.ForeignSchema);
            Assert.AreEqual("contact_type", col.ForeignTable);
            Assert.AreEqual("contact_type_id", col.ForeignColumn);
            Assert.AreEqual("code", col.ForeignRefDataCodeColumn);
            Assert.AreEqual("1", col.DefaultValue);
            Assert.AreEqual("ContactTypeId", col.DotNetName);

            col = tab.Columns[5];
            Assert.AreEqual("gender_id", col.Name);
            Assert.AreEqual("int", col.Type);
            Assert.AreEqual("INT NULL", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.AreEqual(0, col.Scale);
            Assert.AreEqual(10, col.Precision);
            Assert.AreEqual("int", col.DotNetType);
            Assert.IsTrue(col.IsNullable);
            Assert.IsFalse(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsTrue(col.IsForeignRefData);
            Assert.AreEqual(string.Empty, col.ForeignSchema);
            Assert.AreEqual("gender", col.ForeignTable);
            Assert.AreEqual("gender_id", col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);

            col = tab.Columns[6];
            Assert.AreEqual("notes", col.Name);
            Assert.AreEqual("text", col.Type);
            Assert.AreEqual("TEXT NULL", col.SqlType);
            Assert.AreEqual(65535, col.Length);
            Assert.IsNull(col.Scale);
            Assert.IsNull(col.Precision);
            Assert.AreEqual("string", col.DotNetType);
            Assert.IsTrue(col.IsNullable);
            Assert.IsFalse(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.DefaultValue);

            col = tab.Columns[11];
            Assert.AreEqual("contact_type_code", col.Name);
            Assert.IsTrue(col.IsRefData);

            // [Test].[MultiPk]
            tab = tables.Where(x => x.Name == "multi_pk").SingleOrDefault();
            Assert.IsNotNull(tab);
            Assert.AreEqual(string.Empty, tab.Schema);
            Assert.AreEqual("multi_pk", tab.Name);
            Assert.AreEqual("mp", tab.Alias);
            Assert.AreEqual("`multi_pk`", tab.QualifiedName);
            Assert.IsFalse(tab.IsAView);
            Assert.IsFalse(tab.IsRefData);
            Assert.AreEqual(4, tab.Columns.Count);
            Assert.AreEqual(2, tab.PrimaryKeyColumns.Count);
            Assert.AreEqual("MultiPk", tab.DotNetName);
            Assert.AreEqual("MultiPks", tab.PluralName);

            col = tab.Columns[0];
            Assert.AreEqual("part1", col.Name);
            Assert.AreEqual("int", col.Type);
            Assert.AreEqual("INT", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.AreEqual(0, col.Scale);
            Assert.AreEqual(10, col.Precision);
            Assert.AreEqual("int", col.DotNetType);
            Assert.IsFalse(col.IsNullable);
            Assert.IsTrue(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);

            col = tab.Columns[1];
            Assert.AreEqual("part2", col.Name);
            Assert.AreEqual("int", col.Type);
            Assert.AreEqual("INT", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.AreEqual(0, col.Scale);
            Assert.AreEqual(10, col.Precision);
            Assert.AreEqual("int", col.DotNetType);
            Assert.IsFalse(col.IsNullable);
            Assert.IsTrue(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);

            col = tab.Columns[2];
            Assert.AreEqual("value", col.Name);
            Assert.AreEqual("decimal", col.Type);
            Assert.AreEqual("DECIMAL(16, 4) NULL", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.AreEqual(4, col.Scale);
            Assert.AreEqual(16, col.Precision);
            Assert.AreEqual("decimal", col.DotNetType);
            Assert.IsTrue(col.IsNullable);
            Assert.IsFalse(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);

            col = tab.Columns[3];
            Assert.AreEqual("parts", col.Name);
            Assert.AreEqual("int", col.Type);
            Assert.AreEqual("INT NULL", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.AreEqual(0, col.Scale);
            Assert.AreEqual(10, col.Precision);
            Assert.AreEqual("int", col.DotNetType);
            Assert.IsTrue(col.IsNullable);
            Assert.IsFalse(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsTrue(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);

            AssertExtraTypes(tables, "extra_types",
            [
                ("medium_value", "mediumint", "int", false, "int"),
                ("year_value", "year", "short", false, "short"),
                ("bit_flag", "bit", "bool", false, "bool"),
                ("bit_mask", "bit", "ulong", false, "string"),
                ("enum_value", "enum", "string", true, "string"),
                ("set_value", "set", "string", true, "string"),
                ("geom", "geometry", "Geometry", true, "string"),
                ("pt", "point", "Point", true, "string"),
                ("ln", "linestring", "LineString", true, "string"),
                ("poly", "polygon", "Polygon", true, "string"),
                ("multi_pt", "multipoint", "MultiPoint", true, "string"),
                ("multi_ln", "multilinestring", "MultiLineString", true, "string"),
                ("multi_poly", "multipolygon", "MultiPolygon", true, "string"),
                ("geom_coll", "geomcollection", "GeometryCollection", true, "string"),
                ("geog", "point", "Point", true, "string"),
                ("embedding", "vector", "byte[]", true, "string"),
            ]);
        }

        [Test]
        public async Task PostgresSelectSchema()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("PostgresDb");
            var l = UnitTest.GetLogger<DatabaseSchemaTest>();
            var a = new MigrationArgs(MigrationCommand.Drop | MigrationCommand.Create | MigrationCommand.Migrate | MigrationCommand.Schema, cs) { Logger = l }.AddAssembly(typeof(PostgresStuff));
            using var m = new PostgresMigration(a);
            var r = await m.MigrateAsync().ConfigureAwait(false);
            Assert.IsTrue(r);

            using var db = new PostgresDatabase(() => new Npgsql.NpgsqlConnection(cs));
            var tables = await db.SelectSchemaAsync(m).ConfigureAwait(false);
            Assert.IsNotNull(tables);

            // [Test].[ContactType]
            var tab = tables.Where(x => x.Name == "contact_type").SingleOrDefault();
            Assert.IsNotNull(tab);
            Assert.AreEqual("public", tab.Schema);
            Assert.AreEqual("contact_type", tab.Name);
            Assert.AreEqual("ct", tab.Alias);
            Assert.AreEqual("\"public\".\"contact_type\"", tab.QualifiedName);
            Assert.IsFalse(tab.IsAView);
            Assert.IsTrue(tab.IsRefData);
            Assert.AreEqual(5, tab.Columns.Count);
            Assert.AreEqual(1, tab.PrimaryKeyColumns.Count);
            Assert.AreEqual("ContactType", tab.DotNetName);
            Assert.AreEqual("ContactTypes", tab.PluralName);

            var col = tab.Columns[0];
            Assert.AreEqual("contact_type_id", col.Name);
            Assert.AreEqual("integer", col.Type);
            Assert.AreEqual("INTEGER", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.AreEqual(0, col.Scale);
            Assert.AreEqual(32, col.Precision);
            Assert.AreEqual("int", col.DotNetType);
            Assert.IsFalse(col.IsNullable);
            Assert.IsTrue(col.IsPrimaryKey);
            Assert.IsTrue(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);
            Assert.AreEqual("ContactTypeId", col.DotNetName);
            Assert.AreEqual("ContactTypeId", col.DotNetCleanedName);

            col = tab.Columns[1];
            Assert.AreEqual("code", col.Name);
            Assert.AreEqual("character varying", col.Type);
            Assert.AreEqual("CHARACTER VARYING(50)", col.SqlType);
            Assert.AreEqual(50, col.Length);
            Assert.IsNull(col.Scale);
            Assert.IsNull(col.Precision);
            Assert.AreEqual("string", col.DotNetType);
            Assert.IsFalse(col.IsNullable);
            Assert.IsFalse(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsTrue(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);
            Assert.AreEqual("Code", col.DotNetName);

            col = tab.Columns[2];
            Assert.AreEqual("text", col.Name);

            col = tab.Columns[3];
            Assert.AreEqual("sort_order", col.Name);

            col = tab.Columns[4];
            Assert.AreEqual("xmin", col.Name);
            Assert.AreEqual("xid", col.Type);
            Assert.AreEqual("XID", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.AreEqual(0, col.Scale);
            Assert.AreEqual(32, col.Precision);
            Assert.AreEqual("uint", col.DotNetType);
            Assert.IsFalse(col.IsNullable);
            Assert.IsFalse(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsTrue(col.IsRowVersion);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsTrue(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);
            Assert.AreEqual("RowVersion", col.DotNetName);
            Assert.AreEqual("RowVersion", col.DotNetCleanedName);

            // [Test].[Contact]
            tab = tables.Where(x => x.Name == "contact").SingleOrDefault();
            Assert.IsNotNull(tab);
            Assert.AreEqual("public", tab.Schema);
            Assert.AreEqual("contact", tab.Name);
            Assert.AreEqual("c", tab.Alias);
            Assert.AreEqual("\"public\".\"contact\"", tab.QualifiedName);
            Assert.IsFalse(tab.IsAView);
            Assert.IsFalse(tab.IsRefData);
            Assert.AreEqual(13, tab.Columns.Count);
            Assert.AreEqual(1, tab.PrimaryKeyColumns.Count);
            Assert.AreEqual("Contact", tab.DotNetName);
            Assert.AreEqual("Contacts", tab.PluralName);

            col = tab.Columns[0];
            Assert.AreEqual("contact_id", col.Name);
            Assert.AreEqual("integer", col.Type);
            Assert.AreEqual("INTEGER", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.AreEqual(0, col.Scale);
            Assert.AreEqual(32, col.Precision);
            Assert.AreEqual("int", col.DotNetType);
            Assert.IsFalse(col.IsNullable);
            Assert.IsTrue(col.IsPrimaryKey);
            Assert.IsTrue(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);
            Assert.AreEqual("ContactId", col.DotNetName);

            col = tab.Columns[3];
            Assert.AreEqual("date_of_birth", col.Name);
            Assert.AreEqual("date", col.Type);
            Assert.AreEqual("DATE NULL", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.IsNull(col.Scale);
            Assert.AreEqual(0, col.Precision);
            Assert.AreEqual("DateOnly", col.DotNetType);
            Assert.IsTrue(col.IsNullable);
            Assert.IsFalse(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);
            Assert.AreEqual("DateOfBirth", col.DotNetName);

            col = tab.Columns[4];
            Assert.AreEqual("contact_type_id", col.Name);
            Assert.AreEqual("integer", col.Type);
            Assert.AreEqual("INTEGER", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.AreEqual(0, col.Scale);
            Assert.AreEqual(32, col.Precision);
            Assert.AreEqual("int", col.DotNetType);
            Assert.IsFalse(col.IsNullable);
            Assert.IsFalse(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsTrue(col.IsForeignRefData);
            Assert.AreEqual("public", col.ForeignSchema);
            Assert.AreEqual("contact_type", col.ForeignTable);
            Assert.AreEqual("contact_type_id", col.ForeignColumn);
            Assert.AreEqual("code", col.ForeignRefDataCodeColumn);
            Assert.AreEqual("1", col.DefaultValue);
            Assert.AreEqual("ContactTypeId", col.DotNetName);

            col = tab.Columns[5];
            Assert.AreEqual("gender_id", col.Name);
            Assert.AreEqual("integer", col.Type);
            Assert.AreEqual("INTEGER NULL", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.AreEqual(0, col.Scale);
            Assert.AreEqual(32, col.Precision);
            Assert.AreEqual("int", col.DotNetType);
            Assert.IsTrue(col.IsNullable);
            Assert.IsFalse(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsTrue(col.IsForeignRefData);
            Assert.AreEqual("public", col.ForeignSchema);
            Assert.AreEqual("gender", col.ForeignTable);
            Assert.AreEqual("gender_id", col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);

            col = tab.Columns[6];
            Assert.AreEqual("notes", col.Name);
            Assert.AreEqual("text", col.Type);
            Assert.AreEqual("TEXT NULL", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.IsNull(col.Scale);
            Assert.IsNull(col.Precision);
            Assert.AreEqual("string", col.DotNetType);
            Assert.IsTrue(col.IsNullable);
            Assert.IsFalse(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.DefaultValue);

            col = tab.Columns[11];
            Assert.AreEqual("contact_type_code", col.Name);
            Assert.IsTrue(col.IsRefData);

            // [Test].[MultiPk]
            tab = tables.Where(x => x.Name == "multi_pk").SingleOrDefault();
            Assert.IsNotNull(tab);
            Assert.AreEqual("public", tab.Schema);
            Assert.AreEqual("multi_pk", tab.Name);
            Assert.AreEqual("mp", tab.Alias);
            Assert.AreEqual("\"public\".\"multi_pk\"", tab.QualifiedName);
            Assert.IsFalse(tab.IsAView);
            Assert.IsFalse(tab.IsRefData);
            Assert.AreEqual(5, tab.Columns.Count);
            Assert.AreEqual(2, tab.PrimaryKeyColumns.Count);
            Assert.AreEqual("MultiPk", tab.DotNetName);
            Assert.AreEqual("MultiPks", tab.PluralName);

            col = tab.Columns[0];
            Assert.AreEqual("part1", col.Name);
            Assert.AreEqual("integer", col.Type);
            Assert.AreEqual("INTEGER", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.AreEqual(0, col.Scale);
            Assert.AreEqual(32, col.Precision);
            Assert.AreEqual("int", col.DotNetType);
            Assert.IsFalse(col.IsNullable);
            Assert.IsTrue(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);

            col = tab.Columns[1];
            Assert.AreEqual("part2", col.Name);
            Assert.AreEqual("integer", col.Type);
            Assert.AreEqual("INTEGER", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.AreEqual(0, col.Scale);
            Assert.AreEqual(32, col.Precision);
            Assert.AreEqual("int", col.DotNetType);
            Assert.IsFalse(col.IsNullable);
            Assert.IsTrue(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);

            col = tab.Columns[2];
            Assert.AreEqual("value", col.Name);
            Assert.AreEqual("money", col.Type);
            Assert.AreEqual("MONEY NULL", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.IsNull(col.Scale);
            Assert.IsNull(col.Precision);
            Assert.AreEqual("decimal", col.DotNetType);
            Assert.IsTrue(col.IsNullable);
            Assert.IsFalse(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsFalse(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);

            col = tab.Columns[3];
            Assert.AreEqual("parts", col.Name);
            Assert.AreEqual("integer", col.Type);
            Assert.AreEqual("INTEGER NULL", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.AreEqual(0, col.Scale);
            Assert.AreEqual(32, col.Precision);
            Assert.AreEqual("int", col.DotNetType);
            Assert.IsTrue(col.IsNullable);
            Assert.IsFalse(col.IsPrimaryKey);
            Assert.IsFalse(col.IsIdentity);
            Assert.IsNull(col.IdentitySeed);
            Assert.IsNull(col.IdentityIncrement);
            Assert.IsFalse(col.IsUnique);
            Assert.IsTrue(col.IsComputed);
            Assert.IsFalse(col.IsForeignRefData);
            Assert.IsNull(col.ForeignSchema);
            Assert.IsNull(col.ForeignTable);
            Assert.IsNull(col.ForeignColumn);
            Assert.IsNull(col.DefaultValue);

            AssertExtraTypes(tables, "extra_types",
            [
                ("ip_address", "inet", "IPAddress", true, "string"),
                ("network", "cidr", "NpgsqlCidr", false, "string"),
                ("mac_address", "macaddr", "PhysicalAddress", true, "string"),
                ("mac_address8", "macaddr8", "PhysicalAddress", true, "string"),
                ("search_vector", "tsvector", "NpgsqlTsVector", true, "string"),
                ("search_query", "tsquery", "NpgsqlTsQuery", true, "string"),
                ("pt", "point", "NpgsqlPoint", false, "string"),
                ("ln", "line", "NpgsqlLine", false, "string"),
                ("seg", "lseg", "NpgsqlLSeg", false, "string"),
                ("bx", "box", "NpgsqlBox", false, "string"),
                ("pth", "path", "NpgsqlPath", false, "string"),
                ("poly", "polygon", "NpgsqlPolygon", false, "string"),
                ("circ", "circle", "NpgsqlCircle", false, "string"),
                ("bit_flag", "bit", "bool", false, "bool"),
                ("bit_mask", "bit", "BitArray", true, "string"),
                ("bit_varying", "bit varying", "BitArray", true, "string"),
                ("tags", "ARRAY", "string[]", true, "string"),
                ("numbers", "ARRAY", "int[]", true, "string"),
                ("current_mood", "USER-DEFINED", "string", true, "string"),
                ("moods", "ARRAY", "string[]", true, "string"),
                ("int_range", "int4range", "NpgsqlRange<int>", false, "string"),
                ("ts_range", "tstzrange", "NpgsqlRange<DateTime>", false, "string"),
                ("date_range", "daterange", "NpgsqlRange<DateOnly>", false, "string"),
                ("int_multirange", "int4multirange", "NpgsqlRange<int>[]", true, "string"),
                ("attributes", "USER-DEFINED", "Dictionary<string, string?>", true, "string"),
                ("label", "USER-DEFINED", "string", true, "string"),
                ("ci_text", "USER-DEFINED", "string", true, "string"),
                ("geom", "USER-DEFINED", "Point", true, "string"),
                ("geom_any", "USER-DEFINED", "Geometry", true, "string"),
                ("geog", "USER-DEFINED", "Geometry", true, "string"),
                ("embedding", "USER-DEFINED", "Vector", true, "string"),
                ("half_embedding", "USER-DEFINED", "HalfVector", true, "string"),
                ("sparse_embedding", "USER-DEFINED", "SparseVector", true, "string"),
            ]);
        }

        /// <summary>
        /// Asserts the extra (non-primitive) types for the specified table; all mismatches are reported together.
        /// </summary>
        private static void AssertExtraTypes(List<DbTableSchema> tables, string tableName, (string Column, string Type, string DotNetType, bool IsClass, string DataParserType)[] expected)
        {
            var tab = tables.SingleOrDefault(x => x.Name == tableName);
            Assert.IsNotNull(tab, $"Table '{tableName}' not found.");

            var errors = new List<string>();
            foreach (var e in expected)
            {
                var col = tab.Columns.SingleOrDefault(x => x.Name == e.Column);
                if (col is null)
                {
                    errors.Add($"{e.Column}: column not found.");
                    continue;
                }

                var actual = $"{col.Type}|{col.DotNetType}|{col.IsDotNetTypeAClass}|{col.DataParserType}|{col.IsNullable}";
                var expect = $"{e.Type}|{e.DotNetType}|{e.IsClass}|{e.DataParserType}|True";
                if (!string.Equals(actual, expect, StringComparison.OrdinalIgnoreCase))
                    errors.Add($"{e.Column}: expected '{expect}' but was '{actual}' (SqlType: '{col.SqlType}', Native: '{col.NativeSqlType}').");
            }

            Assert.AreEqual(expected.Length + 1, tab.Columns.Count(x => !x.IsRowVersion), "Column count (including the primary key) is unexpected.");
            Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors));
        }
    }
}