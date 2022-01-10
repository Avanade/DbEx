<br/>

![Logo](./images/Logo256x256.png "DbEx")

<br/>

## Introduction

_DbEx_ provides database extensions for both:
- [DbUp-based](#DbUp-based) database migrations; and
- [ADO.NET](#ADO.NET) database access.

<br/>

## Status

[![CI](https://github.com/Avanade/DbEx/workflows/CI/badge.svg)](https://github.com/Avanade/DbEx/actions?query=workflow%3ACI) [![NuGet version](https://badge.fury.io/nu/DbEx.svg)](https://badge.fury.io/nu/DbEx)

The included [change log](CHANGELOG.md) details all key changes per published version.

<br/>

## DbUp-based

[DbUp](https://dbup.readthedocs.io/en/latest/) is a .NET library that is used to deploy changes to relational databases (supports multiple database technologies). It tracks which SQL scripts have been run already, and runs the change scripts in the order specified that are needed to get a database up to date. _DbEx_ provides additional functionality to improve the end-to-end experience of managing database migrations/updates leveraging DbUp.

Traditionally, a [Data-tier Application (DAC)](https://docs.microsoft.com/en-us/sql/relational-databases/data-tier-applications/data-tier-applications) is used to provide a logical means to define all of the SQL Server objects - like tables, views, and instance objects, including logins - associated with a database. A DAC is a self-contained unit of SQL Server database deployment that enables data-tier developers and database administrators to package SQL Server objects into a portable artifact called a DAC package, also known as a DACPAC. This is largely specific to Microsoft SQL Server. Alternatively, there are other tools such as [redgate](https://www.red-gate.com/products/sql-development/sql-toolbelt-essentials/) that may be used. DbUp provides a more explicit approach, one that Microsoft also adopts with the likes of [EF Migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/).

<br/>

## Getting started

The easiest way to get started is to clone the repository and execute `DbEx.Test.Console` project, this will create a database with data.

``` bash
DbEx.Test.Console git:(main)> export cs="Data Source=localhost, 1433;Initial Catalog=DbEx.Console;User id=sa;Password=Xxxxxx@123;TrustServerCertificate=true"
DbEx.Test.Console git:(main)> dotnet run -- -cv cs all
```

Next, create your own console app, follow the structure of `DbEx.Test.Console` project, add reference to https://www.nuget.org/packages/DbEx and your SQL scripts.

Currently, the easiest way of generating scripts from an existing database, is to use the `Generate Scripts` feature of SQL Server Management Studio and copy its output.

<br/>

### Commands (functions)

The _DbEx_ [`DatabaseMigratorBase`](./src/DbEx/Migration/DatabaseMigratorBase.cs) provides the base database provider agnostic capability, with the [`SqlServerMigrator`](./src/DbEx/Migration/SqlServer/SqlServerMigrator.cs) providing the specific Microsoft SQL Server implementation, that automates the functionality as specified by the [`MigrationCommand`](./src/DbEx/Migration/MigrationCommand.cs). One or more commands can be specified, and they will be executed in the order listed.

Command | Description
-|-
`Drop` | Drop the existing database (where it already exists).
`Create` | Create the database (where it does not already exist).
[`Migrate`](#Migrate) | Being the upgrading of a database overtime using order-based migration scripts; the tool leverages the philosophy and NuGet packages of [DbUp](https://dbup.readthedocs.io/en/latest/philosophy-behind-dbup/) to enable.
[`Schema`](#Schema) | There are a number of database schema objects that can be managed outside of the above migrations, that are dropped and (re-)applied to the database using their native `Create` statement.
`Reset` | Resets the database by deleting all existing data (excludes `dbo` and `cdc` schema).
[`Data`](#Data) | There is data, for example *Reference Data* that needs to be applied to a database. This provides a simpler configuration than specifying the required SQL statements directly. This is _also_ useful for setting up Master and Transaction data for the likes of testing scenarios.

Additional commands available are:

Command | Description
-|-
`All` | Performs _all_ the primary commands as follows; `Create`, `Migrate`, `Schema` and `Data`.
`Deploy` | Performs `Migrate` and `Schema`.
`DeployWithData` | Performs `Deploy` and `Data`.
`DropAndAll` | Performs `Drop` and `All`.
`ResetAndAll` | Performs `Reset` and `All` (designed primarily for testing).
`ResetAndData` | Performs `Reset` and `Data` (designed primarily for testing).
`Execute` | Executes the SQL statement(s) passed as additional arguments.
`Script` | Creates a new [`migration`](#Migrate) script file using the defined naming convention.

<br/>

### Migrate

As stated, [DbUp](https://dbup.readthedocs.io/en/latest/) is used enabling a database to be dropped, created and migrated. The migration is managed by tracking order-based migration scripts. It tracks which SQL scripts have been run already, and runs the change scripts that are needed to get the database up to date. 

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

<br/>

#### Reference data

*Reference Data* is treated as a special case. The first column name and value pair are treated as the `Code` and `Text` columns; as defined via the [`DataParserArgs`](./src/DbEx/Migration/Data/DataParserArgs.cs) (see `RefDataCodeColumnName` and `RefDataTextColumnName` properties). 

Where a column is a *Reference Data* reference the reference data code can be specified, with the identifier being determined at runtime (using a sub-query) as it is unlikely to be known at configuration time. The tooling determines this by the column name being suffixed by `Id` a foreign-key constraint being defined; or alternatively a corresponding table name in the same schema or `RefDataAlternateSchema`; example `GenderId` column and corresponding table `Ref.Gender`.

Alternatively, a *Reference Data* reference could be the code itself, typically named XxxCode (e.g. `GenderCode`). This has the advantage of decoupling the reference data references from the underlying identifier. Where data is persisted as JSON then the **code** is used; this would ensure consistency. The primary disadvantage is that the **code** absolutely becomes _immutable_ and therefore not easily changed; for the most part this would not be an issue.

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

[`DbEx`](./src/DbEx/Console/MigratorConsoleBase.cs) has been optimized so that a new console application can reference and inherit the underlying capabilities.

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

<br/>

#### Script command

To simplify the process for the developer _DbEx_ enables the creation of new migration script files into the `Migrations` folder. This will name the script file correctly and output the basic SQL statements to perform the selected function. The date and time stamp will use [DateTime.UtcNow](https://docs.microsoft.com/en-us/dotnet/api/system.datetime.utcnow) as this should avoid conflicts where being co-developed across time zones. 

This requires the usage of the `Script` command, plus zero or more optional arguments where the first is the sub-command (these are will depend on the script being created). The optional arguments must appear in the order listed; where not specified it will default within the script file.

Sub-command | Argument(s) | Description
-|-|-
[N/A](./src/DbEx/Resources/Default_sql.hbs) | N/A | Creates a new empty skeleton script file.
[`Schema`](./src/DbEx/Resources/Schema_sql.hbs) | `Schema` and `Table` | Creates a new table create script file for the named schema and table.
[`Create`](./src/DbEx/Resources/Create_sql.hbs) | `Schema` and `Table` | Creates a new table create script file for the named schema and table.
[`RefData`](./src/DbEx/Resources/RefData_sql.hbs) | `Schema` and `Table` | Creates a new reference data table create script file for the named schema and table.
[`Alter`](./src/DbEx/Resources/Alter_sql.hbs) | `Schema` and `Table` | Creates a new table alter script file for the named schema and table.
[`CdcDb`](./src/DbEx/Resources/CdcDb_sql.hbs) | N/A | Creates a new `sys.sp_cdc_enable_db` script file for the database.
[`Cdc`](./src/DbEx/Resources/Cdc_sql.hbs) | `Schema` and `Table` | Creates a new `sys.sp_cdc_enable_table` script file for the named schema and table.

Examples as follows.

```
dotnet run scriptnew
dotnet run scriptnew create Foo Bar
dotnet run scriptnew refdata Foo Gender
dotnet run scriptnew alter Foo Bar
dotnet run scriptnew cdcdb
dotnet run scriptnew cdc Foo Bar
```

#### Execute command

The execute command allows one or more SQL Statements to be executed directly against the database. This is intended for enabling commands to be executed only. No response other than success or failure will be acknowledged; as such this is not intended for performing queries.

Examples as follows.

dotnet run execute "create schema [Xyz] authorization [dbo]"

<br/>

## ADO.NET 

To simplify generic access to an [ADO.NET](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/) enabled database the following are provided.

Class | Description
-|-
[`Database`](./src/DbEx/Database.cs) | Enables database provider agnostic access to invoke a `StoredProcedure` or `SqlStatement`.
[`DatabaseParameterCollection`](./src/DbEx/DatabaseParameterCollection.cs) | Encapsulates the underlying [`DbParameterCollection`](https://docs.microsoft.com/en-us/dotnet/api/system.data.common.dbparametercollection) to simplify.
[`DatabaseCommand`](./src/DbEx/DatabaseCommand.cs) | Encapsulates the underlying [`DbCommand`](https://docs.microsoft.com/en-us/dotnet/api/system.data.common.dbcommand) to simplify.
[`IMultiSetArgs`](./src/DbEx/IMultiSetArgs.cs) | Provides a simplified means to manage queries which return one or more result sets using a [`DbDataReader`](https://docs.microsoft.com/en-us/dotnet/api/system.data.common.dbdatareader). See also [`MultiSetSingleArgs`](./src/DbEx/MultiSetSingleArgsT.cs) and [`MultiSetCollArgs`](./src/DbEx/MultiSetCollArgsT.cs)
[`DatabaseRecord`](./src/DbEx/DatabaseRecord.cs) | Encapsulates the underlying [`DbDataReader`](https://docs.microsoft.com/en-us/dotnet/api/system.data.common.dbdatareader) to simplify column value access for a given record.
[`IDatabaseMapper`](./src/DbEx/IDatabaseMapper.cs) | Provides a standardized approach for mapping between a [`DatabaseRecord`](./src/DbEx/DatabaseRecord.cs) and a corresponding .NET object.

The [`DatabaseCommand`](./src/DbEx/DatabaseCommand.cs) provides the following key methods to access data.

Method | Description
-|-
`NonQueryAsync` | Executes a non-query command.
`ScalarAsync` | Executes the query and returns the first column of the first row in the result set returned by the query.
`SelectAsync` | Selects none or more items from the first result set.
`SelectMultiSetAsync` | Executes a multi-dataset query command with one or more [`IMultiSetArgs`](./src/DbEx/IMultiSetArgs.cs).
`SelectSingleAsync` | Selects a single item.
`SelectSingleOrDefaultAsync` | Selects a single item or default.
`SelectFirstAsync` | Selects first item.
`SelectFirstOrDefaultAsync` | Selects first item or default.

<br/>

### Infer database schema

Within a code-generation, or other context, the database schema may need to be inferred to understand the basic schema for all tables and their corresponding columns.

The [`Database`](./src/DbEx/Database.cs) class provides a `SelectSchemaAsync` method to return a [`DbTableSchema`](./src/DbEx/Schema/DbTableSchema.cs) list, including the respective columns for each table (see [`DbColumnSchema`](./src/DbEx/Schema/DbColumnSchema.cs)).

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
