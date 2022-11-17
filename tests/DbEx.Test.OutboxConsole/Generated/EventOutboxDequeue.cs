/*
 * This file is automatically generated; any changes will be lost. 
 */

#nullable enable
#pragma warning disable

using CoreEx.Database;
using CoreEx.Database.SqlServer.Outbox;
using CoreEx.Events;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data;

namespace DbEx.Test.OutboxConsole.Data
{
    /// <summary>
    /// Provides the <see cref="EventSendData"/> <see cref="IDatabase">database</see> <i>outbox enqueue</i> <see cref="SendAsync(EventSendData[])"/>. 
    /// </summary>
    public sealed class EventOutboxDequeue : EventOutboxDequeueBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventOutboxDequeue"/> class.
        /// </summary>
        /// <param name="database">The <see cref="IDatabase"/>.</param>
        /// <param name="eventSender">The <see cref="IEventSender"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public EventOutboxDequeue(IDatabase database, IEventSender eventSender, ILogger<EventOutboxDequeue> logger) : base(database, eventSender, logger) { }

        /// <inheritdoc/>
        protected override string DequeueStoredProcedure => "[Outbox].[spEventOutboxDequeue]";
    }
}

#pragma warning restore
#nullable restore