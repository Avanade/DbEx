using OnRamp.Config;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DbEx.Test.OutboxConsole.Config
{
    [CodeGenClass("CodeGen")]
    public sealed class RootConfig : ConfigRootBase<RootConfig>
    {
        [JsonPropertyName("namespaceOutbox")]
        [CodeGenProperty("Namespace", Title = "The Namespace (root) for the Outbox-related .NET artefacts.")]
        public string? NamespaceOutbox { get; set; }

        /// <summary>
        /// Gets or sets the schema name of the event outbox table.
        /// </summary>
        [JsonPropertyName("outboxSchema")]
        [CodeGenProperty("Outbox", Title = "The schema name of the event outbox table.",
            Description = "Defaults to `Outbox` (literal).")]
        public string? OutboxSchema { get; set; }

        /// <summary>
        /// Gets or sets the name of the event outbox table.
        /// </summary>
        [JsonPropertyName("outboxTable")]
        [CodeGenProperty("Outbox", Title = "The name of the event outbox table.",
            Description = "Defaults to `EventOutbox` (literal).")]
        public string? OutboxTable { get; set; }

        /// <summary>
        /// Gets or sets the stored procedure name for the event outbox enqueue.
        /// </summary>
        [JsonPropertyName("outboxEnqueueStoredProcedure")]
        [CodeGenProperty("Outbox", Title = "The stored procedure name for the event outbox enqueue.",
            Description = "Defaults to `spEventOutboxEnqueue` (literal).")]
        public string? OutboxEnqueueStoredProcedure { get; set; }

        /// <summary>
        /// Gets or sets the stored procedure name for the event outbox dequeue.
        /// </summary>
        [JsonPropertyName("outboxDequeueStoredProcedure")]
        [CodeGenProperty("Outbox", Title = "The stored procedure name for the event outbox dequeue.",
            Description = "Defaults to `spEventOutboxDequeue` (literal).")]
        public string? OutboxDequeueStoredProcedure { get; set; }

        /// <inheritdoc/>
        protected override Task PrepareAsync()
        {
            NamespaceOutbox = DefaultWhereNull(NamespaceOutbox, () => "DbEx.Test.OutboxConsole");
            OutboxSchema = DefaultWhereNull(OutboxSchema, () => "Outbox");
            OutboxTable = DefaultWhereNull(OutboxTable, () => "EventOutbox");
            OutboxEnqueueStoredProcedure = DefaultWhereNull(OutboxEnqueueStoredProcedure, () => $"sp{OutboxTable}Enqueue");
            OutboxDequeueStoredProcedure = DefaultWhereNull(OutboxDequeueStoredProcedure, () => $"sp{OutboxTable}Dequeue");
            return Task.CompletedTask;
        }
    }
}