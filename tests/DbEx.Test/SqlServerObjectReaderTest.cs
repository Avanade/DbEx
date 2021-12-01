using DbEx.Migration.SqlServer.Internal;
using NUnit.Framework;
using System;

namespace DbEx.Test
{
    [TestFixture]
    public class SqlServerObjectReaderTest
    {
        [Test]
        public void A100_Read()
        {
            var r = SqlServerObjectReader.Read("XXX", @"/* comments */ create" + Environment.NewLine
                + "type dbo.Blah { }", new string[] { "PROC", "TYPE" }, new string[] { });

            Assert.IsTrue(r.IsValid);
            Assert.AreEqual(r.Type, "TYPE");
            Assert.AreEqual(r.Schema, "dbo");
            Assert.AreEqual(r.Name, "Blah");
        }

        [Test]
        public void A110_Read()
        {
            var r = SqlServerObjectReader.Read("XXX", @"/* comments " + Environment.NewLine
                + " comments */ create" + Environment.NewLine
                + "type Blah { }", new string[] { "PROC", "TYPE" }, new string[] { });

            Assert.IsTrue(r.IsValid);
            Assert.AreEqual(r.Type, "TYPE");
            Assert.AreEqual(r.Schema, "dbo");
            Assert.AreEqual(r.Name, "Blah");
        }

        [Test]
        public void A120_Read()
        {
            var r = SqlServerObjectReader.Read("XXX", @"CREATE" + Environment.NewLine
                + "-- comments" + Environment.NewLine
                + "type -- comments" + Environment.NewLine
                + "[Ref].[Status]", new string[] { "PROC", "TYPE" }, new string[] { });

            Assert.IsTrue(r.IsValid);
            Assert.AreEqual(r.Type, "TYPE");
            Assert.AreEqual(r.Schema, "Ref");
            Assert.AreEqual(r.Name, "Status");
        }

        [Test]
        public void A130_Read()
        {
            var r = SqlServerObjectReader.Read("XXX", @"CREATE FUNCTION IsOK()", new string[] { "FUNCTION", "TYPE" }, new string[] { });

            Assert.IsTrue(r.IsValid);
            Assert.AreEqual(r.Type, "FUNCTION");
            Assert.AreEqual(r.Schema, "dbo");
            Assert.AreEqual(r.Name, "IsOK");
        }
    }
}