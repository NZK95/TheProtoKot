using Microsoft.Data.Sqlite;
using clrhost;

internal sealed class ReportCheckerDatabase
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public ReportCheckerDatabase(string dbPath = "reportCheckerDb/botaccountsdata.db")
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

        using var walCmd = connection.CreateCommand();
        walCmd.CommandText = "PRAGMA journal_mode=WAL;";
        walCmd.ExecuteNonQuery();

        using var cmd = connection.CreateCommand();
        cmd.CommandText =
        @"
        CREATE TABLE IF NOT EXISTS accounts (
            chat_id INTEGER NOT NULL,
            username TEXT NOT NULL DEFAULT '',
            nickname TEXT NOT NULL DEFAULT '',
            account_id INTEGER NOT NULL DEFAULT 0,
            server TEXT NOT NULL DEFAULT '',
            notifications_enabled INTEGER NOT NULL DEFAULT 1,
            UNIQUE(chat_id, username, nickname, account_id, server)
        );

        CREATE TABLE IF NOT EXISTS limits (
            chat_id INTEGER PRIMARY KEY,
            account_limit INTEGER NOT NULL DEFAULT 2
        );

CREATE TABLE IF NOT EXISTS sent_complaints (
    url TEXT PRIMARY KEY
);
        ";
        cmd.ExecuteNonQuery();
    }

    public async Task AddAccountAsync(ReportedAccountRecord account)
    {
        await _writeLock.WaitAsync();
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
                INSERT OR IGNORE INTO accounts (chat_id, username, nickname, account_id, server)
                VALUES ($chatId, $username, $nickname, $accountId, $server);
            ";
            cmd.Parameters.AddWithValue("$chatId", account.ChatId);
            cmd.Parameters.AddWithValue("$username", account.Username);
            cmd.Parameters.AddWithValue("$nickname", account.Nickname);
            cmd.Parameters.AddWithValue("$accountId", account.AccountId);
            cmd.Parameters.AddWithValue("$server", account.Server);

            await cmd.ExecuteNonQueryAsync();
        }
        finally { _writeLock.Release(); }
    }

    public async Task AddAccountWithLimitsAsync(long chatId)
    {
        await _writeLock.WaitAsync();
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO limits (chat_id, account_limit) VALUES ($chatId, 2);";
            cmd.Parameters.AddWithValue("$chatId", chatId);

            await cmd.ExecuteNonQueryAsync();
        }
        finally { _writeLock.Release(); }
    }

    public async Task ToggleNotificationsAsync(long chatId, long accountId)
    {
        await _writeLock.WaitAsync();
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE accounts
                SET notifications_enabled = CASE WHEN notifications_enabled = 1 THEN 0 ELSE 1 END
                WHERE chat_id = $chatId AND account_id = $accountId;";
            cmd.Parameters.AddWithValue("$chatId", chatId);
            cmd.Parameters.AddWithValue("$accountId", accountId);

            await cmd.ExecuteNonQueryAsync();
        }
        finally { _writeLock.Release(); }
    }

    public async Task DeleteAccountAsync(long chatId, long accountId, string nickname)
    {
        await _writeLock.WaitAsync();
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM accounts WHERE chat_id = $chatId AND account_id = $accountId AND nickname = $nickname";
            cmd.Parameters.AddWithValue("$chatId", chatId);
            cmd.Parameters.AddWithValue("$accountId", accountId);
            cmd.Parameters.AddWithValue("$nickname", nickname);

            await cmd.ExecuteNonQueryAsync();
        }
        finally { _writeLock.Release(); }
    }

    public async Task UpdateAccountAsync(long chatId, string username, string nickname, long accountId, string server)
    {
        await _writeLock.WaitAsync();
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
                UPDATE accounts
                SET username = $username,
                    nickname = $nickname,
                    account_id = $accountId,
                    server = $server
                WHERE chat_id = $chatId;
            ";
            cmd.Parameters.AddWithValue("$username", username);
            cmd.Parameters.AddWithValue("$nickname", nickname);
            cmd.Parameters.AddWithValue("$accountId", accountId);
            cmd.Parameters.AddWithValue("$server", server);
            cmd.Parameters.AddWithValue("$chatId", chatId);

            await cmd.ExecuteNonQueryAsync();
        }
        finally { _writeLock.Release(); }
    }

    public async Task SetAccountLimitAsync(long chatId, int limit)
    {
        await _writeLock.WaitAsync();
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO limits (chat_id, account_limit)
                VALUES ($chatId, $limit)
                ON CONFLICT(chat_id) DO UPDATE SET account_limit = $limit;
            ";
            cmd.Parameters.AddWithValue("$chatId", chatId);
            cmd.Parameters.AddWithValue("$limit", limit);

            await cmd.ExecuteNonQueryAsync();
        }
        finally { _writeLock.Release(); }
    }

    public async Task<List<ReportedAccountRecord>> GetAllAccountsAsync()
    {
        var accounts = new List<ReportedAccountRecord>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT chat_id, username, nickname, account_id, server, notifications_enabled FROM accounts;";

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            accounts.Add(new ReportedAccountRecord
            {
                ChatId = reader.GetInt64(0),
                Username = reader.GetString(1),
                Nickname = reader.GetString(2),
                AccountId = reader.GetInt64(3),
                Server = reader.GetString(4),
                NotificationsEnabled = reader.GetInt64(5) == 1
            });
        }

        return accounts;
    }

    public async Task<List<ReportedAccountRecord>> GetAccountsAsync(long chatId)
    {
        var accounts = new List<ReportedAccountRecord>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT chat_id, username, nickname, account_id, server, notifications_enabled
            FROM accounts
            WHERE chat_id = $chatId;
        ";
        cmd.Parameters.AddWithValue("$chatId", chatId);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            accounts.Add(new ReportedAccountRecord
            {
                ChatId = reader.GetInt64(0),
                Username = reader.GetString(1),
                Nickname = reader.GetString(2),
                AccountId = reader.GetInt64(3),
                Server = reader.GetString(4),
                NotificationsEnabled = reader.GetInt64(5) == 1
            });
        }

        return accounts;
    }

    public async Task<int> GetAccountsCountAsync(long chatId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM accounts WHERE chat_id = $chatId;";
        cmd.Parameters.AddWithValue("$chatId", chatId);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<int> GetAccountLimitAsync(long chatId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT account_limit FROM limits WHERE chat_id = $chatId;";
        cmd.Parameters.AddWithValue("$chatId", chatId);

        var result = await cmd.ExecuteScalarAsync();
        return result is null ? 2 : Convert.ToInt32(result);
    }

    public async Task<bool> IsComplaintSentAsync(string url)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sent_complaints WHERE url = $url;";
        cmd.Parameters.AddWithValue("$url", url);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    public async Task MarkComplaintSentAsync(string url)
    {
        await _writeLock.WaitAsync();
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO sent_complaints (url) VALUES ($url);";
            cmd.Parameters.AddWithValue("$url", url);

            await cmd.ExecuteNonQueryAsync();
        }
        finally { _writeLock.Release(); }
    }
}