/*
 * This file is automatically generated; any changes will be lost. 
 */

namespace DbEx.Test.OutboxConsole.Data;

/// <summary>
/// Provides the <see cref="EventSendData"/> <see cref="IDatabase">database</see> <i>outbox enqueue</i> <see cref="SendAsync(EventSendData[])"/>. 
/// </summary>
/// <param name="database">The <see cref="IDatabase"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public sealed class EventOutboxEnqueue(IDatabase database, ILogger<EventOutboxEnqueue> logger) : EventOutboxEnqueueBase(database, logger)
{
    /// <inheritdoc/>
    protected override string EnqueueStoredProcedure => "[Outbox].[spEventOutboxEnqueue]";
}