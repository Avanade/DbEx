<br/>

![Logo](./images/Logo256x256.png "DbEx")

<br/>

## Introduction

_DbEx_ provides database extensions for [DbUp-inspired](#DbUp-inspired) database migrations.

<br/>

## Status

The build status is [![CI](https://github.com/Avanade/DbEx/workflows/CI/badge.svg)](https://github.com/Avanade/DbEx/actions?query=workflow%3ACI) with the NuGet package status as follows, including links to the underlying source code and documentation:

Package | Status | Source & documentation
-|-|-
`DbEx` | [![NuGet version](https://badge.fury.io/nu/DbEx.svg)](https://badge.fury.io/nu/DbEx) | [Link](./src/DbEx)
`DbEx.MySql` | [![NuGet version](https://badge.fury.io/nu/DbEx.MySql.svg)](https://badge.fury.io/nu/DbEx.MySql) | [Link](./src/DbEx.MySql)
`DbEx.SqlServer` | [![NuGet version](https://badge.fury.io/nu/DbEx.SqlServer.svg)](https://badge.fury.io/nu/DbEx.SqlServer) | [Link](./src/DbEx.SqlServer)


The included [change log](CHANGELOG.md) details all key changes per published version.

<br/>

## DbUp-inspired

[DbUp](https://dbup.readthedocs.io/en/latest/) is a .NET library that is used to deploy changes to relational databases (supports multiple database technologies). It tracks which SQL scripts have been run already, and runs the change scripts in the order specified that are needed to get a database up to date. 

Traditionally, a [Data-tier Application (DAC)](https://docs.microsoft.com/en-us/sql/relational-databases/data-tier-applications/data-tier-applications) is used to provide a logical means to define all of the SQL Server objects - like tables, views, and instance objects, including logins - associated with a database. A DAC is a self-contained unit of SQL Server database deployment that enables data-tier developers and database administrators to package SQL Server objects into a portable artifact called a DAC package, also known as a DACPAC. This is largely specific to Microsoft SQL Server. Alternatively, there are other tools such as [redgate](https://www.red-gate.com/products/sql-development/sql-toolbelt-essentials/) that may be used. DbUp provides a more explicit approach, one that Microsoft similarily adopts with the likes of [EF Migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/).

_DbEx_ provides additional functionality to improve the end-to-end experience of managing database migrations/updates leveraging the concepts of DbUp. _DbEx_ prior to version `1.0.14` exclusively leveraged DbUb; however, due to the slow uptake of some key pull requests by the maintainers of _DbUp_ which was starting to impose limitations on _DbEx_. The decision was made to emulate some functionality internally to achieve the functionality goals of _DbEx_. The changes are compatible with the underlying [journaling](./src/DbEx/Migration/IDatabaseJournal.cs) that DbUp leverages (i.e. simulates the same).

<br/>

## Getting started

The easiest way to get started is to clone the repository and execute `DbEx.Test.Console` project, this will create a database with data.

``` bash
DbEx.Test.Console git:(main)> export cs="Data Source=localhost, 1433;Initial Catalog=DbEx.Console;User id=sa;Password=Xxxxxx@123;TrustServerCertificate=true"
DbEx.Test.Console git:(main)> dotnet run -- -cv cs all
```

Next, create your own console app, follow the structure of `DbEx.Test.Console` project, add reference to https://www.nuget.org/packages/DbEx, then add in your SQL scripts.

Currently, the easiest way of generating scripts from an existing database, is to use the `Generate Scripts` feature of _SQL Server Management Studio_ and copy its output.

<br/>

### Commands (functions)

The _DbEx_ [`DatabaseMigrationBase`](./src/DbEx/Migration/DatabaseMigrationBase.cs) provides the base database provider agnostic capability, with the likes of the [`SqlServerMigrator`](./src/DbEx.SqlServer/Migration/SqlServerMigration.cs) providing the specific Microsoft SQL Server implementation. This automates the functionality as specified by the [`MigrationCommand`](./src/DbEx/MigrationCommand.cs). One or more commands can be specified, and they will be executed in the order listed.

Command | Description
-|-
`Drop` | Drop the existing database (where it already exists).
`Create` | Create the database (where it does not already exist).
[`Migrate`](#Migrate) | Being the upgrading of a database overtime using order-based migration scripts; the tool is consistent with the philosophy of [DbUp](https://dbup.readthedocs.io/en/latest/philosophy-behind-dbup/) to enable.
`CodeGen` | Provides opportunity to integrate a code-generation step where applicable (none by default).
[`Schema`](#Schema) | There are a number of database schema objects that can be managed outside of the above migrations, that are dropped and (re-)applied to the database using their native `Create` statement.
`Reset` | Resets the database by deleting all existing data (exclusions can be configured).
[`Data`](#Data) | There is data, for example *Reference Data* that needs to be applied to a database. This provides a simpler configuration than specifying the required SQL statements directly (which is also supported). This is _also_ useful for setting up Master and Transaction data for the likes of testing scenarios.

Additional commands available are:

Command | Description
-|-
`All` | Performs _all_ the primary commands as follows; `Create`, `Migrate`, `CodeGen`, `Schema` and `Data`.
`Database` | Performs `Create`, `Migrate`, `CodeGen` and `Data`.
`Deploy` | Performs `Migrate` and `Schema`.
`DeployWithData` | Performs `Deploy` and `Data`.
`DropAndAll` | Performs `Drop` and `All`.
`DropAndDatabase` | Performs `Drop` and `Database`.
`ResetAndAll` | Performs `Reset` and `All` (designed primarily for testing).
`ResetAndData` | Performs `Reset` and `Data` (designed primarily for testing).
`ResetAndDatabase` | Performs `Reset` and `Database` (designed primarily for testing).
`Execute` | Executes the SQL statement(s) passed as additional arguments.
`Script` | Creates a new [`migration`](#Migrate) script file using the defined naming convention.

<br/>

### Migrate

As stated, the [DbUp](https://dbup.readthedocs.io/en/latest/) approach is used enabling a database to be dropped, created and migrated. The migration is managed by tracking order-based migration scripts. It tracks which SQL scripts have been run already, and runs the change scripts that are needed to get the database up to date. 

Over time there will be more than one script updating a single object, for example a `Table`. In this case the first script operation will be a `Create`, followed by subsequent `Alter` operations. The scripts should be considered immutable, in that they cannot be changed once they have been applied; ongoing changes will need additional scripts.

The migration scripts must be marked as embedded resources, and reside under the `Migrations` folder within the c# project. A naming convention should be used to ensure they are to be executed in the correct order; it is recommended that the name be prefixed by the date and time, followed by a brief description of the purpose. For example: `20181218-081540-create-demo-person-table.sql`

It is recommended that each script be enclosed by a transaction that can be rolled back in the case of error; otherwise, a script could be partially applied and will then need manual intervention to resolve.

_Note_: There are _special case_ scripts that will be executed pre- and post- migrations. In that any scripts ending with `pre.deploy.sql` will always be executed before the migrations are attempted, and any scripts ending with `post.deploy.sql` will always be executed after all the migrations have successfully executed.

<br/>

### Schema

There are some key schema objects that can be dropped and created overtime without causing side-effects. Equally, these objects can be code-generated reducing the effort to create and maintain over time. As such, these objects fall outside of the *Migrations* above.

The currently supported objects are (order specified implies order in which they are applied, and reverse when dropped to allow for dependencies):
1. [Type](https://docs.microsoft.com/en-us/sql/t-sql/statements/create-type-transact-sql)
2. [Function](https://docs.microsoft.com/en-us/sql/t-sql/statements/create-function-transact-sql)
3. [View](https://docs.microsoft.com/en-us/sql/t-sql/statements/create-view-transact-sql)
4. [Procedure](https://docs.microsoft.com/en-us/sql/t-sql/statements/create-procedure-transact-sql)

The schema scripts must be marked as embedded resources, and reside under the `Schema` folder within the c# project. Each script should only contain a single `Create` statement. Each script will be parsed to determine type so that the appropriate order can be applied.

The `Schema` folder is used to encourage the usage of database schemas. Therefore, directly under should be the schema name, for example `dbo` or `Ref`. Then sub-folders for the object types as per [Azure Data Studio](https://docs.microsoft.com/en-au/sql/azure-data-studio/what-is), for example `Functions`, `Stored Procedures` or `Types\User-Defined Table Types`. 

<br/>

### Data

Data can be defined using [YAML](https://en.wikipedia.org/wiki/YAML) to enable simplified configuration that will be used to generate the required SQL statements to apply to the database.

The data specified follows a basic indenting/levelling rule to enable:
1. **Schema** - specifies Schema name.
2. **Table** - specifies the Table name within the Schema; this will be validated to ensure it exists within the database as the underlying table schema (columns) will be inferred. The underyling rows will be [inserted](https://docs.microsoft.com/en-us/sql/t-sql/statements/insert-transact-sql) by default; or alternatively by prefixing with a `$` character a [merge](https://docs.microsoft.com/en-us/sql/t-sql/statements/merge-transact-sql) operation will be performed instead.
3. **Rows** - each row specifies the column name and the corresponding values (except for reference data described below). The tooling will parse each column value according to the underying SQL type.

Finally, SQL script files can also be provided in addition to YAML where explicit SQL is to be executed.

<br/>

#### Reference data

*Reference Data* is treated as a special case. The first column name and value pair are treated as the `Code` and `Text` columns; as defined via the [`DataParserArgs`](./src/DbEx/Migration/Data/DataParserArgs.cs) (see `RefDataCodeColumnName` and `RefDataTextColumnName` properties). 

Where a column is a *Reference Data* reference the reference data code can be specified, with the identifier being determined at runtime (using a sub-query) as it is unlikely to be known at configuration time. The tooling determines this by the column name being suffixed by `Id` and a foreign-key constraint being defined.

Alternatively, a *Reference Data* reference could be the code itself, typically named XxxCode (e.g. `GenderCode`). This has the advantage of decoupling the reference data references from the underlying identifier. Where data is persisted as JSON then the **code** is used; this would ensure consistency. The primary disadvantage is that the **code** absolutely becomes _immutable_ and therefore not easily changed; for the most part this typically is not an issue.

<br/>

#### YAML configuration

Example YAML configuration for *merging* reference data is as follows.

``` YAML
Ref:
  - $Gender:
    - M: Male
    - F: Female
```

Example YAML configuration for *inserting* data (also inferring the `GenderId` from the specified reference data code) is as follows.

``` YAML
Demo:
  - Person:
    - { FirstName: Wendy, LastName: Jones, Gender: F, Birthday: 1985-03-18 }
    - { FirstName: Brian, LastName: Smith, Gender: M, Birthday: 1994-11-07 }
    - { FirstName: Rachael, LastName: Browne, Gender: F, Birthday: 1972-06-28, Street: 25 Upoko Road, City: Wellington }
    - { FirstName: Waylon, LastName: Smithers, Gender: M, Birthday: 1952-02-21 }
  - WorkHistory:
    - { PersonId: 2, Name: Telstra, StartDate: 2015-05-23, EndDate: 2016-04-06 }
    - { PersonId: 2, Name: Optus, StartDate: 2016-04-16 }
```

Additionally, to use an [`IIdentifierGenerator`](./src/DbEx/Migration/Data/IIdentifierGenerator.cs) to generate the identifiers the [`DataParserArgs`](./src/DbEx/Migration/Data/DataParserArgs.cs) `IdentifierGenerator` property must be specified (this defaults to  [`GuidIdentifierGenerator`](./src/DbEx/Migration/Data/GuidIdentifierGenerator.cs)). For this to be used the `^` prefix must be specified for each corresponding table (must opt-in); must occur after `$` merge character where specified. Example as follows.

``` yaml
Ref:
  - $^Gender:
    - { Code: M, Text: Male, TripCode: Male }
Demo:
  - ^Person:
    - { FirstName: Wendy, LastName: Jones, Gender: F, Birthday: 1985-03-18 }
```

Finally, runtime values can be used within the YAML using the value lookup notation; this notation is `^(Key)`. This will either reference the [`DataParserArgs`](./src/DbEx/Migration/Data/DataParserArgs.cs) `RuntimeParameters` property using the specified key. There are two special parameters, being `UserName` and `DateTimeNow`, that reference the same named `DataParserArgs` properties. Where not found the extended notation `^(Namespace.Type.Property.Method().etc, AssemblyName)` is used. Where the `AssemblyName` is not specified then the default `mscorlib` is assumed. The `System` root namespace is optional, i.e. it will be attempted by default. The initial property or method for a `Type` must be `static`, in that the `Type` will not be instantiated. Example as follows.

``` yaml
Demo:
  - Person:
    - { FirstName: Wendy, Username: ^(System.Security.Principal.WindowsIdentity.GetCurrent().Name,System.Security.Principal.Windows), Birthday: ^(DateTimeNow) }
    - { FirstName: Wendy, Username: ^(Beef.ExecutionContext.EnvironmentUsername,Beef.Core), Birthday: ^(DateTime.UtcNow) }
```

<br/>

### Console application

[`DbEx`](./src/DbEx/Console/MigrationConsoleBase.cs) has been optimized so that a new console application can reference and inherit the underlying capabilities.

Where executing directly the default command-line options are as follows.

```
Xxx Database Tool.

Usage: Xxx [options] <command> <args>

Arguments:
  command                    Database migration command.
                             Allowed values are: None, Drop, Create, Migrate, Schema, Deploy, Reset, Data, DeployWithData, All, DropAndAll, ResetAndData, ResetAndAll, Execute, Script.
  args                       Additional arguments; 'Script' arguments (first being the script name) -or- 'Execute' (each a SQL statement to invoke).

Options:
  -?|-h|--help               Show help information.
  -cs|--connection-string    Database connection string.
  -cv|--connection-varname   Database connection string environment variable name.
  -so|--schema-order         Database schema name (multiple can be specified in priority order).
  -o|--output                Output directory path.
  -a|--assembly              Assembly containing embedded resources (multiple can be specified in probing order).
  -eo|--entry-assembly-only  Use the entry assembly only (ignore all other assemblies).
```

The [`DbEx.Test.Console`](./tests/DbEx.Test.Console) demonstrates how this can be leveraged. The command-line arguments need to be passed through to support the standard options. Additional methods exist to specify defaults or change behaviour as required. An example [`Program.cs`](./tests/DbEx.Test.Console/Program.cs) is as follows.

```
using DbEx.Console;
using System.Threading.Tasks;

namespace DbEx.Test.Console
{
    public class Program 
    {
        internal static Task<int> Main(string[] args) => SqlServerMigratorConsole
            .Create<Program>("Data Source=.;Initial Catalog=DbEx.Console;Integrated Security=True")
            .RunAsync(args);
    }
}
```

_Tip:_ To ensure all files are included as embedded resources add the following to the .NET project:

``` xml
<ItemGroup>
  <EmbeddedResource Include="Schema\**\*" />
  <EmbeddedResource Include="Migrations\**\*" />
  <EmbeddedResource Include="Data\**\*" />
</ItemGroup>
```

<br/>

#### Script command

To simplify the process for the developer _DbEx_ enables the creation of new migration script files into the `Migrations` folder. This will name the script file correctly and output the basic SQL statements to perform the selected function. The date and time stamp will use [DateTime.UtcNow](https://docs.microsoft.com/en-us/dotnet/api/system.datetime.utcnow) as this should avoid conflicts where being co-developed across time zones. 

This requires the usage of the `Script` command, plus zero or more optional arguments where the first is the sub-command (these are will depend on the script being created). The optional arguments must appear in the order listed; where not specified it will default within the script file. Depending on the database provider not all of the following will be supported.

Sub-command | Argument(s) | Description
-|-|-
[N/A](./src/DbEx.SqlServer/Resources/ScriptDefault_sql.hbs) | N/A | Creates a new empty skeleton script file.
[`Schema`](./src/DbEx.SqlServer/Resources/ScriptSchema_sql.hbs) | `Schema` and `Table` | Creates a new table create script file for the named schema and table.
[`Create`](./src/DbEx.SqlServer/Resources/ScriptCreate_sql.hbs) | `Schema` and `Table` | Creates a new table create script file for the named schema and table.
[`RefData`](./src/DbEx.SqlServer/Resources/ScriptRefData_sql.hbs) | `Schema` and `Table` | Creates a new reference data table create script file for the named schema and table.
[`Alter`](./src/DbEx.SqlServer/Resources/ScriptAlter_sql.hbs) | `Schema` and `Table` | Creates a new table alter script file for the named schema and table.
[`CdcDb`](./src/DbEx.SqlServer/Resources/ScriptCdcDb_sql.hbs) | N/A | Creates a new `sys.sp_cdc_enable_db` script file for the database.
[`Cdc`](./src/DbEx.SqlServer/Resources/ScriptCdc_sql.hbs) | `Schema` and `Table` | Creates a new `sys.sp_cdc_enable_table` script file for the named schema and table.

Examples as follows.

```
dotnet run script
dotent run script schema Foo
dotnet run script create Foo Bar
dotnet run script refdata Foo Gender
dotnet run script alter Foo Bar
dotnet run script cdcdb
dotnet run script cdc Foo Bar
```

#### Execute command

The execute command allows one or more SQL Statements, and/or Script files, to be executed directly against the database. This is intended for enabling commands to be executed only. No response other than success or failure will be acknowledged; as such this is not intended for performing queries.

Examples as follows.

```
dotnet run execute "create schema [Xyz] authorization [dbo]"
dotnet run execute ./schema/createscehma.sql
```

<br/>

## Infer database schema

Within a code-generation, or other context, the database schema may need to be inferred to understand the basic schema for all tables and their corresponding columns.

The [`Database`](./src/DbEx/DatabaseExtensions.cs) class provides a `SelectSchemaAsync` method to return a [`DbTableSchema`](./src/DbEx/DbSchema/DbTableSchema.cs) list, including the respective columns for each table (see [`DbColumnSchema`](./src/DbEx/DbSchema/DbColumnSchema.cs)).

<br/>

## Other considerations

To simplify the database management here are some further considerations that may make life easier over time; especially where you adopt the philosophy that the underlying busines logic (within the application APIs) is primarily responsible for the consistency of the data; and the data source (the database) is being largely used for storage and advanced query:

- **Nullable everything** - all columns (except) the primary key should be defined as nullable. The business logic should validate the request to ensure data is provided where mandatory. Makes changes to the database schema easier over time without this constraint.
- **Minimise constraints** - do not use database constraints unless absolutely necessary; only leverage where the database is the best and/or most efficient means to perform; i.e. uniqueness. The business logic should validate the request to ensure that any related data is provided, is valid and consistent. 
- **No cross-schema referencing** - avoid referencing across `Schemas` where possible as this will impact the Migrations as part of this tooling; and we should not be using constraints as per prior point. Each schema is considered independent of others except in special cases, such as `dbo` or `sec` (security where used) for example.
- **JSON for schema-less** - where there is data that needs to be persisted, but rarely searched on, a schema-less approach should be considered such that a JSON object is persisted into a single column versus having to define additional tables and/or columns. This can further simplify the database requirements where the data is hierarchical in nature. To enable the [`ObjectToJsonConverter`](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx/Mapping/Converters/ObjectToJsonConverter.cs) and [`AutoMapperObjectToJsonConverter`](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.AutoMapper/Converters/AutoMapperObjectToJsonConverter.cs) can be used within the corresponding mapper to enable.

<br/>


## Other repos

These other _Avanade_ repositories leverage _DbEx_:
- [NTangle](https://github.com/Avanade/NTangle) - Change Data Capture (CDC) code generation tool and runtime.
- [Beef](https://github.com/Avanade/Beef) - Business Entity Execution Framework to enable industralisation of API development.

<br/>

## License

_DbEx_ is open source under the [MIT license](./LICENSE) and is free for commercial use.

<br/>

## Contributing

One of the easiest ways to contribute is to participate in discussions on GitHub issues. You can also contribute by submitting pull requests (PR) with code changes. Contributions are welcome. See information on [contributing](./CONTRIBUTING.md), as well as our [code of conduct](https://avanade.github.io/code-of-conduct/).

<br/>

## Security

See our [security disclosure](./SECURITY.md) policy.

<br/>

## Who is Avanade?

[Avanade](https://www.avanade.com) is the leading provider of innovative digital and cloud services, business solutions and design-led experiences on the Microsoft ecosystem, and the power behind the Accenture Microsoft Business Group.
