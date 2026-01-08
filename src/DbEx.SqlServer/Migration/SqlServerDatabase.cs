namespace DbEx.SqlServer.Migration;

/// <summary>
/// Provides the <see href="https://learn.microsoft.com/en-us/sql/">SQL Server</see> <see cref="IDatabase"/> functionality.
/// </summary>
/// <param name="create"></param>
public class SqlServerDatabase(Func<SqlConnection> create) : Database<SqlConnection>(create, SqlClientFactory.Instance) { }