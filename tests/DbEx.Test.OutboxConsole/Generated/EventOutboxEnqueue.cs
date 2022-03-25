/*
 * This file is automatically generated; any changes will be lost. 
 */

#nullable enable
#pragma warning disable

using DbEx;
using DbEx.SqlServer;
using System.Collections.Generic;
using System.Data;

namespace DbEx.Test.OutboxConsole.Data
{
    /// <summary>
    /// Provides the <see cref="EventSendData"/> <see cref="IDatabase">database</see> <i>outbox enqueue</i> <see cref="SendAsync(EventSendData[])"/>. 
    /// </summary>
    public sealed class EventOutboxEnqueue : EventOutboxEnqueueBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventOutboxEnqueue"/> class.
        /// </summary>
        /// <param name="database">The <see cref="IDatabase"/>.</param>
        public EventOutboxEnqueue(IDatabase database) : base(database) { }

        /// <inheritdoc/>
        public override string DbTvpTypeName => "[Outbox].[udtEventOutboxList]";

        /// <inheritdoc/>
        public override string EnqueueStoredProcedure => "[Outbox].[spEventOutboxEnqueue]";
    }
}

#pragma warning restore
#nullable restore