using CoreEx.Database.SqlServer;
using CoreEx.Events;
using DbEx.Migration;
using DbEx.SqlServer.Migration;
using DbEx.Test.OutboxConsole.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DbEx.Test
{
    [TestFixture]
    public class SqlServerOutboxTest
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("ConsoleDb");
            var l = UnitTest.GetLogger<SqlServerOutboxTest>();
            var a = new MigrationArgs(MigrationCommand.DropAndAll, cs) { Logger = l }.AddAssembly(typeof(Console.Program), typeof(DbEx.Test.OutboxConsole.Program));
            using var m = new SqlServerMigration(a);
            m.Args.DataParserArgs.Parameters.Add("DefaultName", "Bazza");
            m.Args.DataParserArgs.RefDataColumnDefaults.Add("SortOrder", i => 1);
            await m.MigrateAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task A100_EnqueueDequeue()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("ConsoleDb");
            var l = UnitTest.GetLogger<SqlServerOutboxTest>();

            using var db = new SqlServerDatabase(() => new SqlConnection(cs));
            await db.SqlStatement("DELETE FROM [Outbox].[EventOutbox]").NonQueryAsync().ConfigureAwait(false);

            var esd = new EventSendData
            {
                Id = "1234",
                Timestamp = new DateTimeOffset(2020, 01, 08, 08, 59, 30, TimeSpan.FromMinutes(360)),
                Subject = "subject",
                Action = "action",
                Type = "subject.action",
                Source = new Uri("/source", UriKind.Relative),
                CorrelationId = "corrid",
                TenantId = "tenant",
                Key = "kiwi",
                Destination = "queue-name",
                ETag = "etag",
                PartitionKey = "partition-key",
                Data = new BinaryData("binary-data-value"),
                Attributes = new Dictionary<string, string> { { "key", "value" } }
            };

            var eoe = new EventOutboxEnqueue(db, UnitTest.GetLogger<EventOutboxEnqueue>());
            await eoe.SendAsync(new EventSendData[] { esd }).ConfigureAwait(false);

            var ims = new InMemorySender();
            var eod = new EventOutboxDequeue(db, ims, UnitTest.GetLogger<EventOutboxDequeue>());
            await eod.DequeueAndSendAsync(10);

            var events = ims.GetEvents();
            Assert.AreEqual(1, events.Length);

            var e = events[0];
            Assert.IsNotNull(e);
            Assert.AreEqual("1234", e.Id);
            Assert.AreEqual(new DateTimeOffset(2020, 01, 08, 08, 59, 30, TimeSpan.FromMinutes(360)), e.Timestamp);
            Assert.AreEqual("subject", e.Subject);
            Assert.AreEqual("action", e.Action);
            Assert.AreEqual("subject.action", e.Type);
            Assert.AreEqual(new Uri("/source", UriKind.Relative), e.Source);
            Assert.AreEqual("corrid", e.CorrelationId);
            Assert.AreEqual("tenant", e.TenantId);
            Assert.AreEqual("kiwi", e.Key);
            Assert.AreEqual("queue-name", e.Destination);
            Assert.AreEqual("etag", e.ETag);
            Assert.AreEqual("partition-key", e.PartitionKey);
            Assert.AreEqual("binary-data-value", e.Data.ToString());

            Assert.IsNotNull(e.Attributes);
            Assert.AreEqual(1, e.Attributes.Count);
            Assert.IsTrue(e.Attributes.ContainsKey("key"));
            Assert.AreEqual("value", e.Attributes["key"]);
        }

        [Test]
        public async Task A110_EnqueueDequeue_MaxDequeueSize()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("ConsoleDb");
            var l = UnitTest.GetLogger<SqlServerOutboxTest>();

            using var db = new SqlServerDatabase(() => new SqlConnection(cs));
            await db.SqlStatement("DELETE FROM [Outbox].[EventOutbox]").NonQueryAsync().ConfigureAwait(false);

            var eoe = new EventOutboxEnqueue(db, UnitTest.GetLogger<EventOutboxEnqueue>());
            await eoe.SendAsync(new EventSendData[]
            {
                new EventSendData { Id = "1", PartitionKey = null, Destination = null },
                new EventSendData { Id = "2", PartitionKey = "apples", Destination = null },
                new EventSendData { Id = "3", PartitionKey = null, Destination = "queue" },
                new EventSendData { Id = "4", PartitionKey = "apples", Destination = "queue" }
            }).ConfigureAwait(false);

            var ims = new InMemorySender();
            var eod = new EventOutboxDequeue(db, ims, UnitTest.GetLogger<EventOutboxDequeue>());

            Assert.AreEqual(3, await eod.DequeueAndSendAsync(3, null, null));
            var events = ims.GetEvents();
            Assert.AreEqual(3, events.Length);
            Assert.AreEqual("1", events[0].Id);
            Assert.AreEqual("2", events[1].Id);
            Assert.AreEqual("3", events[2].Id);
            ims.Reset();

            Assert.AreEqual(1, await eod.DequeueAndSendAsync(3, null, null));
            events = ims.GetEvents();
            Assert.AreEqual(1, events.Length);
            Assert.AreEqual("4", events[0].Id);
            ims.Reset();

            Assert.AreEqual(0, await eod.DequeueAndSendAsync(3, null, null));
            events = ims.GetEvents();
            Assert.AreEqual(0, events.Length);
        }

        [Test]
        public async Task A120_EnqueueDequeue_PartitionKey_Destination_Selection()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("ConsoleDb");
            var l = UnitTest.GetLogger<SqlServerOutboxTest>();

            using var db = new SqlServerDatabase(() => new SqlConnection(cs));
            await db.SqlStatement("DELETE FROM [Outbox].[EventOutbox]").NonQueryAsync().ConfigureAwait(false);

            var eoe = new EventOutboxEnqueue(db, UnitTest.GetLogger<EventOutboxEnqueue>());
            await eoe.SendAsync(new EventSendData[]
            {
                new EventSendData { Id = "1", PartitionKey = null, Destination = null },
                new EventSendData { Id = "2", PartitionKey = "apples", Destination = null },
                new EventSendData { Id = "3", PartitionKey = null, Destination = "queue" },
                new EventSendData { Id = "4", PartitionKey = "apples", Destination = "queue" }
            }).ConfigureAwait(false);

            var ims = new InMemorySender();
            var eod = new EventOutboxDequeue(db, ims, UnitTest.GetLogger<EventOutboxDequeue>());

            await eod.DequeueAndSendAsync(10, "bananas", "queue");
            var events = ims.GetEvents();
            Assert.AreEqual(0, events.Length);
            ims.Reset();

            await eod.DequeueAndSendAsync(10, "apples", "topic");
            events = ims.GetEvents();
            Assert.AreEqual(0, events.Length);
            ims.Reset();

            await eod.DequeueAndSendAsync(10, "apples", "queue");
            events = ims.GetEvents();
            Assert.AreEqual(1, events.Length);
            Assert.AreEqual("4", events[0].Id);
            ims.Reset();

            await eod.DequeueAndSendAsync(10, "apples", null);
            events = ims.GetEvents();
            Assert.AreEqual(1, events.Length);
            Assert.AreEqual("2", events[0].Id);
            ims.Reset();

            await eod.DequeueAndSendAsync(10, null, "queue");
            events = ims.GetEvents();
            Assert.AreEqual(1, events.Length);
            Assert.AreEqual("3", events[0].Id);
            ims.Reset();

            await eod.DequeueAndSendAsync(10, null, null);
            events = ims.GetEvents();
            Assert.AreEqual(1, events.Length);
            Assert.AreEqual("1", events[0].Id);
        }

        [Test]
        public async Task B100_EnqueueDequeue_PrimarySender_Success()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("ConsoleDb");
            var l = UnitTest.GetLogger<SqlServerOutboxTest>();

            using var db = new SqlServerDatabase(() => new SqlConnection(cs));
            await db.SqlStatement("DELETE FROM [Outbox].[EventOutbox]").NonQueryAsync().ConfigureAwait(false);

            var eoe = new EventOutboxEnqueue(db, UnitTest.GetLogger<EventOutboxEnqueue>());
            var pims = new InMemorySender();
            eoe.SetPrimaryEventSender(pims);
            await eoe.SendAsync(new EventSendData[] { new EventSendData { Id = "1", PartitionKey = null, Destination = null } }).ConfigureAwait(false);

            var events = pims.GetEvents();
            Assert.AreEqual(1, events.Length);
            Assert.AreEqual("1", events[0].Id);

            var ims = new InMemorySender();
            var eod = new EventOutboxDequeue(db, ims, UnitTest.GetLogger<EventOutboxDequeue>());

            // Should have updated as dequeued already; therefore, nothing to dequeue.
            Assert.AreEqual(0, await eod.DequeueAndSendAsync(10, null, null));
            events = ims.GetEvents();
            Assert.AreEqual(0, events.Length);
        }

        [Test]
        public async Task B110_EnqueueDequeue_PrimarySender_Error()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("ConsoleDb");
            var l = UnitTest.GetLogger<SqlServerOutboxTest>();

            using var db = new SqlServerDatabase(() => new SqlConnection(cs));
            await db.SqlStatement("DELETE FROM [Outbox].[EventOutbox]").NonQueryAsync().ConfigureAwait(false);

            var eoe = new EventOutboxEnqueue(db, UnitTest.GetLogger<EventOutboxEnqueue>());
            eoe.SetPrimaryEventSender(new TestSender());
            await eoe.SendAsync(new EventSendData[] { new EventSendData { Id = "1", PartitionKey = null, Destination = null } }).ConfigureAwait(false);

            var ims = new InMemorySender();
            var eod = new EventOutboxDequeue(db, ims, UnitTest.GetLogger<EventOutboxDequeue>());

            // Should have updated as enqueued; therefore, need to be dequeued.
            Assert.AreEqual(1, await eod.DequeueAndSendAsync(10, null, null));
            var events = ims.GetEvents();
            Assert.AreEqual(1, events.Length);
            Assert.AreEqual("1", events[0].Id);
        }

        [Test]
        public async Task B120_EnqueueDequeue_PrimarySender_EventSendException()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("ConsoleDb");
            var l = UnitTest.GetLogger<SqlServerMigrationTest>();

            using var db = new SqlServerDatabase(() => new SqlConnection(cs));
            await db.SqlStatement("DELETE FROM [Outbox].[EventOutbox]").NonQueryAsync().ConfigureAwait(false);

            var eoe = new EventOutboxEnqueue(db, UnitTest.GetLogger<EventOutboxEnqueue>());
            eoe.SetPrimaryEventSender(new TestSenderFail());
            await eoe.SendAsync(new EventSendData[] 
            { 
                new() { Id = "1", PartitionKey = null, Destination = "A" },
                new() { Id = "2", PartitionKey = null, Destination = "B" },
                new() { Id = "3", PartitionKey = null, Destination = "A" },
                new() { Id = "4", PartitionKey = null, Destination = "B" }
            }).ConfigureAwait(false);

            var ims = new InMemorySender();
            var eod = new EventOutboxDequeue(db, ims, UnitTest.GetLogger<EventOutboxDequeue>());

            // Should have updated as enqueued; therefore, need to be dequeued.
            Assert.AreEqual(2, await eod.DequeueAndSendAsync(10, null, null));
            var events = ims.GetEvents();
            Assert.AreEqual(2, events.Length);
            Assert.AreEqual("2", events[0].Id);
            Assert.AreEqual("4", events[1].Id);
        }

        private class TestSender : IEventSender
        {
            public event EventHandler AfterSend;

            public Task SendAsync(IEnumerable<EventSendData> events, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        }

        private class TestSenderFail : IEventSender
        {
            public event EventHandler AfterSend;

            public Task SendAsync(IEnumerable<EventSendData> events, CancellationToken cancellationToken = default)
            {
                var elist = events.ToArray();
                throw new EventSendException("Oh no that's not good.", new EventSendData[] { elist[1], elist[3] });
            }
        }
    }
}