/*
 * This file is automatically generated; any changes will be lost. 
 */

namespace DbEx.Test.OutboxConsole.Data;

/// <summary>
/// Provides the <see cref="EventSendData"/> <see cref="IDatabase">database</see> <i>outbox enqueue</i> <see cref="SendAsync(EventSendData[])"/>. 
/// </summary>
public sealed class EventOutboxEnqueue : EventOutboxEnqueueBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventOutboxEnqueue"/> class.
    /// </summary>
    /// <param name="database">The <see cref="IDatabase"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    public EventOutboxEnqueue(IDatabase database, ILogger<EventOutboxEnqueue> logger) : base(database, logger) { }

    /// <inheritdoc/>
    protected override string DbTvpTypeName => "[Outbox].[udtEventOutboxList]";

    /// <inheritdoc/>
    protected override string EnqueueStoredProcedure => "[Outbox].[spEventOutboxEnqueue]";
}