namespace DbEx.MySql.Migration;

/// <summary>
/// Provides <see href="https://dev.mysql.com/">MySQL</see> database access functionality.
/// </summary>
/// <param name="create"></param>
public class MySqlDatabase(Func<MySqlConnection> create) : Database<MySqlConnection>(create, MySqlClientFactory.Instance) { }