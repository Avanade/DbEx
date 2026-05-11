namespace DbEx.Postgres.Migration;

/// <summary>
/// Provides <see href="https://www.npgsql.org/">Npgsql (PostgreSQL)</see> database access functionality.
/// </summary>
/// <param name="create"></param>
public class PostgresDatabase(Func<NpgsqlConnection> create) : Database<NpgsqlConnection>(create, NpgsqlFactory.Instance) { }