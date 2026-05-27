using Microsoft.Data.Sqlite;

internal sealed class MainDatabase
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _dbSemaphore = new(1, 1);

    public MainDatabase(string dbPath = "mainDB/botmaindata.db")
    {
        var baseDir = AppContext.BaseDirectory;
        var basePath = Path.Combine(baseDir, dbPath);

        _connectionString = $"Data Source={basePath};Cache=Shared;";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
        PRAGMA journal_mode=WAL;
        PRAGMA synchronous=NORMAL;

        CREATE TABLE IF NOT EXISTS Users (
            ChatId INTEGER PRIMARY KEY,
            Username TEXT,
            LastMessage TEXT,
            CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            UpdatedAt DATETIME
        );

        CREATE INDEX IF NOT EXISTS idx_users_username ON Users(Username);
    ";

        cmd.ExecuteNonQuery();
    }

    public async Task UpdateSessionFields(long chatId, string? lastMessage, DateTime? updatedAt)
    {
        await _dbSemaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            UPDATE Users
            SET 
                LastMessage = $lastMessage,
                UpdatedAt = $updatedAt
            WHERE ChatId = $chatId
        ";

            cmd.Parameters.AddWithValue("$chatId", chatId);
            cmd.Parameters.AddWithValue("$lastMessage", lastMessage ?? "");
            cmd.Parameters.AddWithValue("$updatedAt", updatedAt);

            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _dbSemaphore.Release();
        }
    }

    public async Task SaveSessionAsync(UserSession session)
    {
        await _dbSemaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO Users(ChatId, Username, LastMessage, CreatedAt, UpdatedAt)
            VALUES ($chatId, $username, $lastMessage, $createdAt, $updatedAt)
            ON CONFLICT(ChatId) DO UPDATE SET
                Username=$username,
                LastMessage=$lastMessage,
                CreatedAt=$createdAt,
                UpdatedAt=$updatedAt
        ";

            cmd.Parameters.AddWithValue("$chatId", session.ChatId);
            cmd.Parameters.AddWithValue("$username", session.TelegramUsername ?? "N/A");
            cmd.Parameters.AddWithValue("$lastMessage", session.LastMessage ?? "");
            cmd.Parameters.AddWithValue("$createdAt", session.CreatedAt);
            cmd.Parameters.AddWithValue("$updatedAt", session.UpdatedAt);

            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _dbSemaphore.Release();
        }
    }

    public async Task<List<UserSession>> GetAllUsersAsync()
    {
        await _dbSemaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT ChatId, Username, LastMessage, CreatedAt, UpdatedAt FROM Users";

            var users = new List<UserSession>();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(new UserSession
                {
                    ChatId = reader.GetInt64(0),
                    TelegramUsername = reader.IsDBNull(1) ? "N/A" : reader.GetString(1),
                    LastMessage = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    CreatedAt = reader.GetDateTime(3),
                    UpdatedAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4)
                });
            }

            return users;
        }
        finally
        {
            _dbSemaphore.Release();
        }
    }
}