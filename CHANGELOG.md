# Change log

Represents the **NuGet** versions.

## v2.5.4
- *Fixed:* Updated `CoreEx` to version `3.21.0`.
- *Fixed:* Updated `DataParser` to set column with JSON (`JsonElement.GetRawText`) value versus throwing an exception when the JSON is an object or an array.

## v2.5.3
- *Fixed:* Updated `CoreEx` to version `3.20.0`.
- *Fixed:* Fixed logging of SQL statements to include the source: `FILE`, `RES` (embedded resource) or `SQL` (specified statement).

## v2.5.2
- *Fixed:* Updated `CoreEx` to version `3.18.0`.
  - Includes removal of `Azure.Identity` dependency; related to `https://github.com/advisories/GHSA-wvxc-855f-jvrv`.

## v2.5.1
- *Fixed:* Updated `CoreEx` to version `3.15.0`.
- *Fixed* Simplify event outbox C# code-generation templates for primary constructor usage.

## v2.5.0
- *Enhancement:* Added [PostgreSQL](https://www.postgresql.org/) database migrations support.
- *Enhancement:* Added `DateOnly` and `TimeOnly` support (requires `net7.0`+) (see also `MigrationArgs.EmitDotNetDateOnly` and `MigrationArgs.EmitDotNetTimeOnly` to explicitly enable).
- *Enhamcement:* Improved the `MigrationArgs` support throughout to simplify usage, and improve configurablility and consistency; enabling greater flexibility to control the migration process/activities.
- *Internal*: 
  - All `throw new ArgumentNullException` checking migrated to the `xxx.ThrowIfNull` extension method equivalent.
  - All	_Run Code Analysis_ issues resolved.

## v2.4.0
- *Enhancement:* Added `MigrationAssemblyArgs` to allow for the specification of zero or more `Data` folder names.
- *Fixed:* Updated `CoreEx` to version `3.9.0`.

## v2.3.15
- *Fixed:* Updated `CoreEx` to version `3.8.0`.
- *Fixed:* Updated `OnRamp` to version `2.0.0` which necessitated internal change from `Newtonsoft.Json` (now deprecated) to `System.Text.Json`; additionally, the `DataParser` was refactored accordingly.

## v2.3.14
- *Fixed:* All related package dependencies updated to latest.

## v2.3.13
- *Fixed:* Updated `CoreEx` to version `3.6.1`.
- *Fixed:* Added `net8.0` support.

## v2.3.12
- *Fixed:* The `OutBox.EventOutbox` table clustered/non-clustered indexes changed to simplify default implementation. Implementors of this capability should review the indexing, etc., based on usage to optimize. 
	- _Note:_ it is also expected that the _Outbox_ tables are regularly purged, i.e. dequeued events should be removed. This is the responsibility of the implementor to perform as required.

## v2.3.11
- *Enhancement:* `DataParser.ParseJsonAsync` added to support JSON data file parsing in addition to the existing YAML. Files (embedded resources) can be mixed and matched as required.
- *Enhancement:* `DataParser` supports specified schema of `*` to provide `DataConfig` that is applied to all tables in the YAML/JSON file.
- *Enhancement:* `DataParser` supports hierarchical data (i.e parent/child) where the child row(s) are specified as a parent column value (must be an array). Will also attempt to update the child related column from the parent primary key where not specified to simplify specification.
- *Fixed:* `DataParser` supports `bit` values of `0` and `1`, in addition to `false` and `true` respectively.
- *Fixed:* `DataParser` constraint of table only being specified once in the YAML/JSON file has been removed; this is now supported. The underlying database insert or merge operations will be consolidated and executed in the sequence of initial specification.

## v2.3.10
- *Fixed:* `DbTableSchema`, `DbColumnSchema` and `DataParserArgs` now correctly support the full range of by-convention properties (e.g. `RowVersion`, `TenantId` and `IsDeleted`). Both the `SqlServerSchemaConfig` and `MySqlSchemaConfig` updated to default names as appropriate. Additional properties added to `Db*Schema` to support additional code-generation scenarios, etc.

## v2.3.9
- *Fixed:* The YAML-based `MigrationCommand.Data` logic previously set the `CreatedBy`, `CreatedDate`, `UpdatedBy` and `UpdatedDate` (or specified equivalent) regardless of whether the data was being inserted or merged (insert/update). This has been corrected such that the appropriate values are only set for the specific type of operation being performed; i.e. `Created*`-only or `Updated*`-only.
- *Fixed:* Enabled additional command-line arguments to be passed for `MigrationCommand.CodeGen` to enable inherited usage flexibility. Removed `MigrationArgsBase.ScriptName` and `MigrationArgsBase.ScriptArguments` and included within existing `MigrationArgsBase.Parameters` as singular dictionary of key/value pairs (simplification).

## v2.3.8
- *Fixed:* `SqlServerMigration` has been fixed to handle column size of `MAX` correctly.

## v2.3.7
- *Fixed:* `SqlServerMigration` updated to correct the `DataResetFilterPredicate` to exclude all tables within schema `cdc`, and exclude all tables within the `dbo` schema where the table name starts with `sys`. This is to ensure that the internal Change Data tables are not reset, and that any SQL Server system tables are not inadvertently reset.

## v2.3.6
- *Enhancement:* Updated `CoreEx` to version `3.3.0`.

## v2.3.5
- *Enhancement:* Updated `CoreEx` to version `3.0.0`.
- *Enhancement:* Updated all dependent packages to latest versions.
- *Enhancement:* Added `net6.0` and `net7.0` support in addition to [.NET Standard](https://learn.microsoft.com/en-us/dotnet/standard/net-standard#when-to-target-net50-or-net60-vs-netstandard) to the `DbEx` packages. This will allow access to additional features per version where required, and overall performance improvements.
- *Enhancement:* Included C# code-generation templates updated; target `net6.0`+ only.

## v2.3.4
- *Fixed:* `MigrationArgsBase.Assemblies` internal list management simplified; all order/sequencing managed within `DatabaseMigrationBase` implementation to limit issues. Added a new `AddAssemblyAfter` to support explicit positioning where needed.

## v2.3.3
- *Fixed:* The unquoting of identifiers whilst parsing schema objects resulted in an empty identifier where the quote was not specified; i.e. for SQL Server `[dbo].[name]` worked, whereas, `dbo.name` did not; this has been corrected.

## v2.3.2
- *Fixed:* The `DotNetType` and `SqlType` property value determination has been improved; removed explicit `Prepare` to simplify.
- *Fixed:* Added `MigrationArgsBase.AcceptPrompts` to programmatically set the equivalent of the `--accept-prompts` command-line option. 

## v2.3.1
- *Fixed:* Added `IDisposable` to `DatabaseMigrationBase` to ensure underlying database connections are correctly disposed (via `IDatabase.Dispose`).
- *Fixed:* `DatabaseJournal` updated to use `DatabaseMigrationBase.ReplaceSqlRuntimeParameters` versus own limited implementation.
- *Fixed:* Throw `InvalidOperationException` versus _errant_ `NullReferenceException` where required embedded resource is not found.

## v2.3.0
- *Enhancement:* Support the execution of `*.post.database.create.sql` migration scripts that will _only_ get invoked after the creation of the database (i.e. a potential one-time only execution).

## v2.2.0
- *Enhancement:* Enable `Parameters` to be passed via the command line; either adding, or overridding any pre-configured values. Use `-p|--param Name=Value` syntax; e.g. `--param JournalSchema=dbo`.
- *Enhancement:* Enable moustache syntax property placeholder replacements (e.g`{{ParameterName}}`), from the `Parameters`, within SQL scripts to allow changes during execution into the database at runtime.
- *Enhancement:* Added command-line confirmation prompt for a `Drop` or `Reset` as these are considered highly destructive actions. Supports `--accept-prompts` option to bypass prompts within scripted scenarios.

## v2.1.1
- *Fixed:* Multibyte support added to the `DataParser` insert and merge for SQL Server strings using the `N` prefix.

## v2.1.0
- *Enhancement:* Added `DataParserArgs.ColumnDefaults` so that _any_ table column(s) can be defaulted where required (where not directly specified).
- *Enhancement:* Improved help text to include the schema command and arguments.

## v2.0.0
- *Enhancement:* Added MySQL database migrations.
- *Note:* Given the extent of this and previous change a major version change is warranted (published version `v1.1.1` should be considered as deprecated as a result).

## v1.1.1
- *Enhancement:* **Breaking change:** Refactored the implementation/internals/naming to be largely database agnostic, have improved extensibility, to simplify introduction of additional databases (beyond current SQL Server implementation). As part of this exercise `DbUp` was re-introduced to provide the likes of SQL command parsing. The database connection management and implementation of `IDatabaseJournal` remains custom (using `DbUp` convention to enable). The `SqlServerMigrationConsole` implementation moved to `DbEx.Console.SqlServer` for separation and consistency.

## v1.0.18
- *Fixed:* Corrected issue where comments were removed from the SQL statement when executed against the database; i.e. they were missing from the likes of stored procedures, etc.
- *Fixed:* Schema create template updated to remove transaction wrap which was invalid.
- *Enhancement:* Existing namespace `Schema` renamed to `DbSchema`.
- *Enhancement:* Updated the outbox code generation templates to support the `Key` column.

## v1.0.17
- *Fixed:* Updated to `CoreEx` version `1.0.9` and `OnRamp` version `1.0.6`.

## v1.0.16
- *Fixed:* SQL using batch, i.e. `GO` statement, were not being split and executed (indepently) correctly.
- *Fixed:* Standard pattern of `CancellationToken` added to all `Async` methods.

## v1.0.15
- *Fixed:* `Reset` command updated to load embedded SQL resource correctly.

## v1.0.14
- *Enhancement:* Removed `DbUp` package dependencies and implemented equivalent (basics) that is compatible with `[dbo].[SchemaVersion]` journal management. Primary reason is related to the slow uptake of pull requests by the maintainers of `DbUp` that imposes limitations on `DbEx`.
- *Fixed:* `DbTypeMapper` updated to support `SMALLDATETIME` and `IMAGE` Microsoft Sql Server types.

## v1.0.13
- *Fixed:* `Int32.ToGuid` extension method changed to `DataValueConverter.IntToGuid` to be more explicit.

## v1.0.12
- *Fixed:* `Reset` command fixed to load embedded resource file correctly.

## v1.0.11
- *Enhancement:* Updated to `CoreEx.Database` version `1.0.7`.

## v1.0.10
- *Enhancement:* Removed generic database functionality as this has been ported to `CoreEx.Database`. `DbEx` is now focused on tooling only.

## v1.0.9
- *Enhancement:* Updated the `EventOutboxEnqueueBase` to handle new `EventSendException` and enqueue each individual message as either sent or unsent within the outbox.
- *Fixed:* Updated to `CoreEx` version `1.0.5`.

## v1.0.8
- *Fixed:* Updated to `CoreEx` version `1.0.3`.

## v1.0.7
- *Fixed:* Previous version `1.0.6` fix was incorrect; Data import order should not have been reversed. This previous change has been corrected. 

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