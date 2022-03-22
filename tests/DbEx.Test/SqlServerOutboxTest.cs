﻿using NUnit.Framework;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using DbEx.Test.OutboxConsole.Data;
using CoreEx.Events;
using System;
using System.Collections.Generic;
using DbEx.Migration.SqlServer;

namespace DbEx.Test
{
    [TestFixture]
    public class SqlServerOutboxTest
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("ConsoleDb");
            var l = UnitTest.GetLogger<SqlServerMigratorTest>();
            var m = new SqlServerMigrator(cs, Migration.MigrationCommand.DropAndAll, l, typeof(Console.Program).Assembly, typeof(DbEx.Test.OutboxConsole.Program).Assembly);
            await m.MigrateAsync().ConfigureAwait(false);
        }

        [Test]
        public async Task A100_EnqueueDequeue()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("ConsoleDb");
            var l = UnitTest.GetLogger<SqlServerMigratorTest>();

            using var db = new Database<SqlConnection>(() => new SqlConnection(cs));
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
                Destination = "queue-name",
                ETag = "etag",
                PartitionKey = "partition-key",
                Data = new BinaryData("binary-data-value"),
                Attributes = new Dictionary<string, string> { { "key", "value" } }
            };

            var eoe = new EventOutboxEnqueue(db);
            await eoe.SendAsync(esd).ConfigureAwait(false);

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
            var l = UnitTest.GetLogger<SqlServerMigratorTest>();

            using var db = new Database<SqlConnection>(() => new SqlConnection(cs));
            await db.SqlStatement("DELETE FROM [Outbox].[EventOutbox]").NonQueryAsync().ConfigureAwait(false);

            var eoe = new EventOutboxEnqueue(db);
            await eoe.SendAsync(
                new EventSendData { Id = "1", PartitionKey = null, Destination = null },
                new EventSendData { Id = "2", PartitionKey = "apples", Destination = null },
                new EventSendData { Id = "3", PartitionKey = null, Destination = "queue" },
                new EventSendData { Id = "4", PartitionKey = "apples", Destination = "queue" }).ConfigureAwait(false);

            var ims = new InMemorySender();
            var eod = new EventOutboxDequeue(db, ims, UnitTest.GetLogger<EventOutboxDequeue>());

            Assert.AreEqual(3, await eod.DequeueAndSendAsync(3, null, null));
            var events = ims.GetEvents();
            Assert.AreEqual(3, events.Length);
            Assert.AreEqual("1", events[0].Id);
            Assert.AreEqual("2", events[1].Id);
            Assert.AreEqual("3", events[2].Id);
            ims.Clear();

            Assert.AreEqual(1, await eod.DequeueAndSendAsync(3, null, null));
            events = ims.GetEvents();
            Assert.AreEqual(1, events.Length);
            Assert.AreEqual("4", events[0].Id);
            ims.Clear();

            Assert.AreEqual(0, await eod.DequeueAndSendAsync(3, null, null));
            events = ims.GetEvents();
            Assert.AreEqual(0, events.Length);
        }

        [Test]
        public async Task A120_EnqueueDequeue_PartitionKey_Destination_Selection()
        {
            var cs = UnitTest.GetConfig("DbEx_").GetConnectionString("ConsoleDb");
            var l = UnitTest.GetLogger<SqlServerMigratorTest>();

            using var db = new Database<SqlConnection>(() => new SqlConnection(cs));
            await db.SqlStatement("DELETE FROM [Outbox].[EventOutbox]").NonQueryAsync().ConfigureAwait(false);

            var eoe = new EventOutboxEnqueue(db);
            await eoe.SendAsync(
                new EventSendData { Id = "1", PartitionKey = null, Destination = null },
                new EventSendData { Id = "2", PartitionKey = "apples", Destination = null },
                new EventSendData { Id = "3", PartitionKey = null, Destination = "queue" },
                new EventSendData { Id = "4", PartitionKey = "apples", Destination = "queue" }).ConfigureAwait(false);

            var ims = new InMemorySender();
            var eod = new EventOutboxDequeue(db, ims, UnitTest.GetLogger<EventOutboxDequeue>());

            await eod.DequeueAndSendAsync(10, "bananas", "queue");
            var events = ims.GetEvents();
            Assert.AreEqual(0, events.Length);
            ims.Clear();

            await eod.DequeueAndSendAsync(10, "apples", "topic");
            events = ims.GetEvents();
            Assert.AreEqual(0, events.Length);
            ims.Clear();

            await eod.DequeueAndSendAsync(10, "apples", "queue");
            events = ims.GetEvents();
            Assert.AreEqual(1, events.Length);
            Assert.AreEqual("4", events[0].Id);
            ims.Clear();

            await eod.DequeueAndSendAsync(10, "apples", null);
            events = ims.GetEvents();
            Assert.AreEqual(1, events.Length);
            Assert.AreEqual("2", events[0].Id);
            ims.Clear();

            await eod.DequeueAndSendAsync(10, null, "queue");
            events = ims.GetEvents();
            Assert.AreEqual(1, events.Length);
            Assert.AreEqual("3", events[0].Id);
            ims.Clear();

            await eod.DequeueAndSendAsync(10, null, null);
            events = ims.GetEvents();
            Assert.AreEqual(1, events.Length);
            Assert.AreEqual("1", events[0].Id);
        }
    }
}