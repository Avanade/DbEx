// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.Events;
using CoreEx.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DbEx.SqlServer
{
    /// <summary>
    /// Provides the base <see cref="EventSendData"/> <see cref="IDatabase">database</see> <i>outbox enqueue</i> <see cref="IEventSender.SendAsync(EventSendData[])"/>. 
    /// </summary>
    /// <remarks>By default the events are first sent/enqueued to the datatbase outbox, then a secondary out-of-process dequeues and sends. This can however introduce unwanted latency depending on the frequency in which the secondary process
    /// performs the dequeue and send, as this is essentially a polling-based operation. To improve (minimize) latency, the primary <see cref="IEventSender"/> can be specified using <see cref="SetPrimaryEventSender(IEventSender)"/>. This will
    /// then be used to send the events immediately, and where successful, they will be audited in the database as dequeued event(s); versus on error (as a backup), where they will be enqueued for the out-of-process dequeue and send (as per default).</remarks>
    public abstract class EventOutboxEnqueueBase : IEventSender
    {
        private IEventSender? _eventSender;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventOutboxEnqueueBase"/> class.
        /// </summary>
        /// <param name="database">The <see cref="IDatabase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public EventOutboxEnqueueBase(IDatabase database, ILogger<EventOutboxEnqueueBase> logger)
        { 
            Database = database ?? throw new ArgumentNullException(nameof(database));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the <see cref="IDatabase"/>.
        /// </summary>
        protected IDatabase Database { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Gets the database type name for the <see cref="TableValuedParameter"/>.
        /// </summary>
        protected abstract string DbTvpTypeName { get; }

        /// <summary>
        /// Gets the event outbox <i>enqueue</i> stored procedure name.
        /// </summary>
        protected abstract string EnqueueStoredProcedure { get; }

        /// <summary>
        /// Gets the column name for the <see cref="EventDataBase.Id"/> property within the event outbox table.
        /// </summary>
        /// <remarks>Defaults to '<c>EventId</c>'.</remarks>
        protected virtual string EventIdColumnName => "EventId";

        /// <summary>
        /// Gets or sets the default partition key.
        /// </summary>
        /// <remarks>Defaults to '<c>$default</c>'. This will ensure that there is always a value recorded in the database.</remarks>
        public string DefaultPartitionKey { get; set; } = "$default";

        /// <summary>
        /// Gets or sets the default destination name.
        /// </summary>
        /// <remarks>Defaults to '<c>$default</c>'. This will ensure that there is always a value recorded in the database.</remarks>
        public string DefaultDestination { get; set; } = "$default";

        /// <summary>
        /// Sets the optional <see cref="IEventSender"/> to act as the primary <see cref="IEventSender"/> where <i>outbox enqueue</i> is to provide backup/audit capabilities.
        /// </summary>
        public void SetPrimaryEventSender(IEventSender eventSender)
        {
            if (eventSender != null & eventSender is EventOutboxEnqueueBase)
                throw new ArgumentException($"{nameof(SetPrimaryEventSender)} value must not be of Type {nameof(EventOutboxEnqueueBase)}.", nameof(eventSender));

            _eventSender = eventSender;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="events"><inheritdoc/></param>
        /// <remarks>Executes the <see cref="EnqueueStoredProcedure"/> to <i>send / enqueue</i> the <paramref name="events"/> to the underlying database outbox tables.</remarks>
        public async Task SendAsync(params EventSendData[] events)
        {
            if (events == null || !events.Any())
                return;

            Stopwatch sw = Stopwatch.StartNew();
            var setEventsAsDequeued = _eventSender != null;
            if (setEventsAsDequeued)
            {
                try
                {
                    await _eventSender!.SendAsync(events).ConfigureAwait(false);
                    sw.Stop();
                    Logger.LogDebug("{EventCount} event(s) were sent successfully. [Sender={Sender}, Elapsed={Elapsed}ms]", events.Length, _eventSender.GetType().Name, sw.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    setEventsAsDequeued = false;
                    Logger.LogWarning(ex, "{EventCount} event(s) were unable to be sent successfully; will be forwarded (sent/enqueued) to the datatbase outbox for an out-of-process send: {ErrorMessage} [Sender={Sender}, Elapsed={Elapsed}ms]",
                        events.Length, ex.Message, _eventSender!.GetType().Name, sw.ElapsedMilliseconds);
                }
            }

            sw = Stopwatch.StartNew();
            await Database.StoredProcedure(EnqueueStoredProcedure, p => p.Param("@SetEventsAsDequeued", setEventsAsDequeued).AddTableValuedParameter("@EventList", CreateTableValuedParameter(events))).NonQueryAsync().ConfigureAwait(false);
            sw.Stop();
            Logger.LogDebug("{EventCount} event(s) were enqueued. [Sender={Sender}, SetEventsAsDequeued={SetAsDequeued}, Elapsed={Elapsed}ms]", events.Length, GetType().Name, setEventsAsDequeued, sw.ElapsedMilliseconds);
        }

        /// <inheritdoc/>
        public TableValuedParameter CreateTableValuedParameter(IEnumerable<EventSendData> list)
        {
            var dt = new DataTable();
            dt.Columns.Add(EventIdColumnName, typeof(string));
            dt.Columns.Add(nameof(EventSendData.Destination), typeof(string));
            dt.Columns.Add(nameof(EventSendData.Subject), typeof(string));
            dt.Columns.Add(nameof(EventSendData.Action), typeof(string));
            dt.Columns.Add(nameof(EventSendData.Type), typeof(string));
            dt.Columns.Add(nameof(EventSendData.Source), typeof(string));
            dt.Columns.Add(nameof(EventSendData.Timestamp), typeof(DateTimeOffset));
            dt.Columns.Add(nameof(EventSendData.CorrelationId), typeof(string));
            dt.Columns.Add(nameof(EventSendData.TenantId), typeof(string));
            dt.Columns.Add(nameof(EventSendData.PartitionKey), typeof(string));
            dt.Columns.Add(nameof(EventSendData.ETag), typeof(string));
            dt.Columns.Add(nameof(EventSendData.Attributes), typeof(byte[]));
            dt.Columns.Add(nameof(EventSendData.Data), typeof(byte[]));

            var tvp = new TableValuedParameter(DbTvpTypeName, dt);
            foreach (var item in list)
            {
                var attributes = item.Attributes == null || item.Attributes.Count == 0 ? new BinaryData(Array.Empty<byte>()) : JsonSerializer.Default.SerializeToBinaryData(item.Attributes);
                tvp.AddRow(item.Id, item.Destination ?? DefaultDestination ?? throw new InvalidOperationException($"The {nameof(DefaultDestination)} must have a non-null value."),
                    item.Subject, item.Action, item.Type, item.Source, item.Timestamp, item.CorrelationId, item.TenantId,
                    item.PartitionKey ?? DefaultPartitionKey ?? throw new InvalidOperationException($"The {nameof(DefaultPartitionKey)} must have a non-null value."),
                    item.ETag, attributes.ToArray(), item.Data?.ToArray());
            }

            return tvp;
        }
    }
}