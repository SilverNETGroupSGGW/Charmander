using Microsoft.Data.Sqlite;

namespace Charmander.Api.Repositories;

public interface IDeviceTokenRepository
{
  Task InitializeAsync(CancellationToken cancellationToken = default);

  Task<long> RegisterAsync(string token, CancellationToken cancellationToken = default);

  Task<bool> ExistsAsync(string token, CancellationToken cancellationToken = default);
}

public sealed class DeviceTokenRepository : IDeviceTokenRepository
{
  private readonly string _connectionString;

  public DeviceTokenRepository(string connectionString = "Data Source=devices.db")
  {
    _connectionString = connectionString;
  }

  public async Task InitializeAsync(CancellationToken cancellationToken = default)
  {
    await using var conn = OpenConnection();
    var cmd = conn.CreateCommand();
    cmd.CommandText = "CREATE TABLE IF NOT EXISTS device_tokens (token TEXT PRIMARY KEY)";
    await cmd.ExecuteNonQueryAsync(cancellationToken);
  }

  public async Task<long> RegisterAsync(string token, CancellationToken cancellationToken = default)
  {
    await using var conn = OpenConnection();

    var insert = conn.CreateCommand();
    insert.CommandText = "INSERT OR IGNORE INTO device_tokens (token) VALUES ($token)";
    insert.Parameters.AddWithValue("$token", token);
    await insert.ExecuteNonQueryAsync(cancellationToken);

    return await GetCountAsync(conn, cancellationToken);
  }

  public async Task<bool> ExistsAsync(string token, CancellationToken cancellationToken = default)
  {
    await using var conn = OpenConnection();
    var lookup = conn.CreateCommand();
    lookup.CommandText = "SELECT 1 FROM device_tokens WHERE token = $token";
    lookup.Parameters.AddWithValue("$token", token);
    return await lookup.ExecuteScalarAsync(cancellationToken) is not null;
  }

  private static async Task<long> GetCountAsync(
      SqliteConnection conn,
      CancellationToken cancellationToken)
  {
    var count = conn.CreateCommand();
    count.CommandText = "SELECT COUNT(*) FROM device_tokens";
    return (long)(await count.ExecuteScalarAsync(cancellationToken) ?? 0L);
  }

  private SqliteConnection OpenConnection()
  {
    var conn = new SqliteConnection(_connectionString);
    conn.Open();
    return conn;
  }
}
