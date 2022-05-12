# Change log

Represents the **NuGet** versions.

## v1.0.9
- *Enhancement:* Updated the `EventOoutboxEnqueueBase` to handle new `EventSendException` and enqueue each individual message as either sent or unsent within the outbox.
- *Fixed:* Updated to `CoreEx` version `1.0.5`.

## v1.0.8
- *Fixed:* Updated to `CoreEx` version `1.0.3`.

## v1.0.7
- *Fixed:* Previous version v1.0.6 fix was incorrect; Data import order should not have been reversed. This preivous change has been corrected. 

## v1.0.6
- *Fixed:* [Issue 12](https://github.com/Avanade/DbEx/issues/12) fixed. Data import order has been reversed.

## v1.0.5
- *Enhancement:* The `EventOutboxEnqueueBase` is the SQL Server event outbox enqueue `IEventSender`. To minimize send latency (increasing real-time event delivery) a primary (alternate) `IEventSender` can be specified (`SetPrimaryEventSender`). This changes the event behaviour whereby the events will be sent via the specified primary first, then enqueued only where the primary fails. The events will still be written to the event outbox but as sent for audit purposes.

## v1.0.4
- *Enhancement:* Integrated SQL Server-based `EventOutbox` code-generation (both database and C#) into _DbEx_ to enable re-use and consistency.

## v1.0.3
- *Enhancement:* Changed the project to be .NET Standard 2.1 versus targeting specific framework version. This has had the side-effect of losing the ability to execute directly from the command-line. Given this should typically be inherited and then executed, this functionality loss is considered a minor inconvenience.

## v1.0.2
- *Fixed:* [Issue 7](https://github.com/Avanade/DbEx/issues/7) fixed. Documentation corrected and support for SQL files for both `Data` and `Execute` commands added.

## v1.0.1
- *New:* Initial publish to GitHub/NuGet. This was originally harvested from, and will replace, the core database tooling within [Beef](https://github.com/Avanade/Beef/tree/master/tools/Beef.Database.Core).