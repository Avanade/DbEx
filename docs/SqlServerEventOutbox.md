# SQL Server Event Outbox

**This is a work in progress...**

To enable a consistent implemenation (and re-use) an implementation of the [Event Outbox pattern](https://microservices.io/patterns/data/transactional-outbox.html) specifically for SQL Server based around the [`EventSendData`](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx/Events/EventSendData.cs) as enabled by [_CoreEx_](https://github.com/Avanade/CoreEx) is provided.

The code-generation is enabled by [_OnRamp_](https://github.com/Avanade/onramp) leveraging the following [templates](https://github.com/Avanade/onramp#templates). The [`DbEx.Test.OutboxConsole`](../tests/DbEx.Test.OutboxConsole) demonstrates usage.

Template | Description | Example
-|-|-
[`SchemaEventOutbox_sql.hbs`](../src/DbEx/Templates/SqlServer/SchemaEventOutbox_sql.hbs) | Outbox database schema. | See [example](../tests/DbEx.Test.OutboxConsole/Migrations/100-create-outbox-schema.sql).
[`TableEventOutbox_sql.hbs`](../src/DbEx/Templates/SqlServer/TableEventOutbox_sql.hbs) | Event outbox table. | See [example](../tests/DbEx.Test.OutboxConsole/Migrations/101-create-outbox-eventoutbox-table.sql).
[`TableEventOutboxData_sql.hbs`](../src/DbEx/Templates/SqlServer/TableEventOutboxData_sql.hbs) | Event outbox table. | See [example](../tests/DbEx.Test.OutboxConsole/Migrations/102-create-outbox-eventoutboxdata-table.sql).
[`UdtEventOutbox_sql.hbs`](../src/DbEx/Templates/SqlServer/UdtEventOutbox_sql.hbs) | Event outbox user-defined table type. | See [example](../tests/DbEx.Test.OutboxConsole/Schema/Outbox/Types/User-Defined%20Table%20Types/Generated/udtEventOutboxList.sql).
[`SpEventOutboxEnqueue_sql.hbs`](../src/DbEx/Templates/SqlServer/SpEventOutboxEnqueue_sql.hbs) | Event outbox enqueue stored procedure. | See [example](../tests/DbEx.Test.OutboxConsole/Schema/Outbox/Stored%20Procedures/Generated/spEventOutboxEnqueue.sql).
[`SpEventOutboxDequeue_sql.hbs`](../src/DbEx/Templates/SqlServer/SpEventOutboxDequeue_sql.hbs) | Event outbox dequeue stored procedure. | See [example](../tests/DbEx.Test.OutboxConsole/Schema/Outbox/Stored%20Procedures/Generated/spEventOutboxDequeue.sql).
[`EventOutboxEnqueue_cs.hbs`](../src/DbEx/Templates/SqlServer/EventOutboxEnqueue_cs.hbs) | Event outbox enqueue (.NET C#); inherits capabilities from [`EventOutboxEnqueueBase`](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.Database/SqlServer/EventOutboxEnqueueBase.cs). | See [example](../tests/DbEx.Test.OutboxConsole/Generated/EventOutboxEnqueue.cs).
[`EventOutboxEnqueue_cs.hbs`](../src/DbEx/Templates/SqlServer/EventOutboxDequeue_cs.hbs) | Event outbox dequeue (.NET C#); inherits capabilities from [`EventOutboxDequeueBase`](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.Database/SqlServer/EventOutboxDequeueBase.cs). | See [example](../tests/DbEx.Test.OutboxConsole/Generated/EventOutboxDequeue.cs).

<br/>

### Event Outbox Enqueue

The [`EventOutboxDequeueBase`](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.Database/SqlServer/EventOutboxEnqueueBase.cs) provides the base [`IEventSender`](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx/Events/IEventSender.cs) send/enqueue capabilities.

By default the events are first sent/enqueued to the datatbase outbox, then a secondary out-of-process dequeues and sends. This can however introduce unwanted latency depending on the frequency in which the secondary process performs the dequeue and send, as this is essentially a polling-based operation. To improve (minimize) latency, the primary `IEventSender` can be specified using the `SetPrimaryEventSender` method. This will then be used to send the events immediately, and where successful, they will be audited in the database as dequeued event(s); versus on error (as a backup), where they will be enqueued for the out-of-process dequeue and send (as per default).

<br/>