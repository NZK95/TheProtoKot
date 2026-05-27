using Microsoft.Data.Sqlite;
using clrhost;

internal sealed class EventsDatabase
{
    private readonly string _connectionString;

    public EventsDatabase(string dbPath = "eventsDb/boteventsdata.db")
    {
        var baseDir = AppContext.BaseDirectory;
        var basePath = Path.Combine(baseDir, dbPath);
        _connectionString = $"Data Source={basePath};Cache=Shared;";
        Initialize();
    }

    private void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText =
        @"
            CREATE TABLE IF NOT EXISTS users (
                user_id INTEGER PRIMARY KEY,
                events_ids TEXT NOT NULL DEFAULT ''
            );
            ";
        cmd.ExecuteNonQuery();
    }
    public async Task AddUserAsync(long userId, string eventsIds = "")
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText =
        @"
    INSERT OR IGNORE INTO users (user_id, events_ids)
    VALUES ($userId, $eventsIds);
    ";
        cmd.Parameters.AddWithValue("$userId", userId);
        cmd.Parameters.AddWithValue("$eventsIds", eventsIds);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteUserAsync(long userId)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM users WHERE user_id = $userId;";
        cmd.Parameters.AddWithValue("$userId", userId);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateUserEventsAsync(long userId, string eventsIds)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE users SET events_ids = $events WHERE user_id = $userId;";
        cmd.Parameters.AddWithValue("$events", eventsIds);
        cmd.Parameters.AddWithValue("$userId", userId);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<UserRecord>> GetAllUsersAsync()
    {
        var users = new List<UserRecord>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT user_id, events_ids FROM users;";

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            users.Add(new UserRecord
            {
                UserId = reader.GetInt64(0),
                EventsIds = reader.GetString(1)
            });
        }

        return users;
    }

    public async Task<UserRecord?> GetUserAsync(long userId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText =
        @"
            SELECT user_id, events_ids
            FROM users
            WHERE user_id = $userId;
            ";
        cmd.Parameters.AddWithValue("$userId", userId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new UserRecord
        {
            UserId = reader.GetInt64(0),
            EventsIds = reader.GetString(1)
        };
    }
}
