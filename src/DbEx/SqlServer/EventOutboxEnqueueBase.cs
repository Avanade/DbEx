// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.Events;
using CoreEx.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DbEx.SqlServer
{
    /// <summary>
    /// Provides the base <see cref="EventSendData"/> <see cref="IDatabase">database</see> <i>outbox enqueue</i> <see cref="IEventSender.SendAsync(EventSendData[])"/>. 
    /// </summary>
    public abstract class EventOutboxEnqueueBase : IEventSender
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventOutboxEnqueueBase"/> class.
        /// </summary>
        /// <param name="database">The <see cref="IDatabase"/>.</param>
        public EventOutboxEnqueueBase(IDatabase database) => Database = database ?? throw new ArgumentNullException(nameof(database));

        /// <summary>
        /// Gets the <see cref="IDatabase"/>.
        /// </summary>
        public IDatabase Database { get; }

        /// <summary>
        /// Gets the database type name for the <see cref="TableValuedParameter"/>.
        /// </summary>
        public abstract string DbTvpTypeName { get; }

        /// <summary>
        /// Gets the event outbox <i>enqueue</i> stored procedure name.
        /// </summary>
        public abstract string EnqueueStoredProcedure { get; }

        /// <summary>
        /// Gets the column name for the <see cref="EventDataBase.Id"/> property within the event outbox table.
        /// </summary>
        /// <remarks>Defaults to '<c>EventId</c>'.</remarks>
        public virtual string EventIdColumnName => "EventId";

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

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="events"><inheritdoc/></param>
        /// <remarks>Executes the <see cref="EnqueueStoredProcedure"/> to <i>send / enqueue</i> the <paramref name="events"/> to the underlying database outbox tables.</remarks>
        public async Task SendAsync(params EventSendData[] events)
        {
            if (events == null || !events.Any())
                return;

            await Database.StoredProcedure(EnqueueStoredProcedure, p => p.AddTableValuedParameter("@EventList", CreateTableValuedParameter(events))).NonQueryAsync().ConfigureAwait(false);
        }
    }
}