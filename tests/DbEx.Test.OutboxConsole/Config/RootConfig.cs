using Newtonsoft.Json;
using OnRamp.Config;
using System.Threading.Tasks;

namespace DbEx.Test.OutboxConsole.Config
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed class RootConfig : ConfigRootBase<RootConfig>
    {
        [JsonProperty("namespaceOutbox", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [CodeGenProperty("Namespace", Title = "The Namespace (root) for the Outbox-related .NET artefacts.")]
        public string? NamespaceOutbox { get; set; }

        /// <summary>
        /// Gets or sets the schema name of the event outbox table.
        /// </summary>
        [JsonProperty("outboxSchema", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [CodeGenProperty("Outbox", Title = "The schema name of the event outbox table.",
            Description = "Defaults to `Outbox` (literal).")]
        public string? OutboxSchema { get; set; }

        /// <summary>
        /// Gets or sets the name of the event outbox table.
        /// </summary>
        [JsonProperty("outboxTable", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [CodeGenProperty("Outbox", Title = "The name of the event outbox table.",
            Description = "Defaults to `EventOutbox` (literal).")]
        public string? OutboxTable { get; set; }

        /// <summary>
        /// Gets or sets the stored procedure name for the event outbox enqueue.
        /// </summary>
        [JsonProperty("outboxEnqueueStoredProcedure", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [CodeGenProperty("Outbox", Title = "The stored procedure name for the event outbox enqueue.",
            Description = "Defaults to `spEventOutboxEnqueue` (literal).")]
        public string? OutboxEnqueueStoredProcedure { get; set; }

        /// <summary>
        /// Gets or sets the stored procedure name for the event outbox dequeue.
        /// </summary>
        [JsonProperty("outboxDequeueStoredProcedure", DefaultValueHandling = DefaultValueHandling.Ignore)]
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