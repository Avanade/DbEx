﻿using CoreEx.Database.MySql;
using CoreEx.Database.SqlServer;
using DbEx.Migration;
using DbEx.MySql;
using DbEx.MySql.Migration;
using DbEx.SqlServer;
using DbEx.SqlServer.Migration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

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
            var tables = await db.SelectSchemaAsync(new SqlServerSchemaConfig("DbEx.Console")).ConfigureAwait(false);
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
            Assert.AreEqual(4, tab.Columns.Count);
            Assert.AreEqual(1, tab.PrimaryKeyColumns.Count);

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
            Assert.AreEqual(8, tab.Columns.Count);
            Assert.AreEqual(1, tab.PrimaryKeyColumns.Count);

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
            Assert.AreEqual("DateTime", col.DotNetType);
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

            col = tab.Columns[6];
            Assert.AreEqual("TenantId", col.Name);

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
            Assert.AreEqual(4, tab.Columns.Count);
            Assert.AreEqual(1, tab.PrimaryKeyColumns.Count);

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
            var tables = await db.SelectSchemaAsync(new MySqlSchemaConfig("dbex_test")).ConfigureAwait(false);
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
            Assert.AreEqual(7, tab.Columns.Count);
            Assert.AreEqual(1, tab.PrimaryKeyColumns.Count);

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

            col = tab.Columns[3];
            Assert.AreEqual("date_of_birth", col.Name);
            Assert.AreEqual("date", col.Type);
            Assert.AreEqual("DATE NULL", col.SqlType);
            Assert.IsNull(col.Length);
            Assert.IsNull(col.Scale);
            Assert.IsNull(col.Precision);
            Assert.AreEqual("DateTime", col.DotNetType);
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
        }
    }
}