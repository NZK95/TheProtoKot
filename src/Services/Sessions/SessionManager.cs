using System.Collections.Concurrent;
using System.Text;
using Telegram.Bot.Types;

internal sealed class SessionManager
{
    public readonly ConcurrentDictionary<long, UserSession> UsersCache;
    public readonly MainDatabase Sqlite;

    private SessionManager(MainDatabase sqlite)
    {
        Sqlite = sqlite;
        UsersCache = new ConcurrentDictionary<long, UserSession>();
    }

    public static async Task<SessionManager> CreateAsync(MainDatabase sqlite)
    {
        var manager = new SessionManager(sqlite);

        var usersFromDb = await sqlite.GetAllUsersAsync();
        foreach (var user in usersFromDb)
        {
            manager.UsersCache.TryAdd(user.ChatId, new UserSession
            {
                ChatId = user.ChatId,
                TelegramUsername = user.TelegramUsername,
                LastMessage = user.LastMessage,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            });
        }

        return manager;
    }

    public async Task<UserSession> GetSessionAsync(long chatId, Update update)
    {
        if (UsersCache.TryGetValue(chatId, out var cachedSession))
        {
            cachedSession.UpdatedAt = DateTime.UtcNow;
            UpdateSession(cachedSession);
            return cachedSession;
        }

        var session = new UserSession
        {
            ChatId = chatId,
            TelegramUsername = update?.Message?.From?.Username ?? "N/A",
            CreatedAt = DateTime.UtcNow,
            LastMessage = "",
            UpdatedAt = DateTime.UtcNow
        };

        UsersCache.TryAdd(chatId, session);
        await Sqlite.SaveSessionAsync(session);
        return session;
    }

    public void UpdateSession(UserSession session)
    {
        UsersCache.AddOrUpdate(session.ChatId, session, (_, __) => session);
    }

    public async Task ClearUserDataAsync(UserSession session, string pathToResult = "")
    {
        if (!string.IsNullOrEmpty(pathToResult) && File.Exists(pathToResult))
            File.Delete(pathToResult);

        session.ClearData();
        UpdateSession(session);
    }

    public long? GetChatIdByUsername(string username)
    {
        var result = UsersCache.Values.Where(v => v.TelegramUsername == username).FirstOrDefault();
        return result is null ? null : result.ChatId;
    }

    public string GetUsernameByChatId(long chatId)
    {
        var result = UsersCache.Values.Where(v => v.ChatId == chatId).FirstOrDefault();
        return result is null ? "N/A" : result.TelegramUsername ?? "N/A";
    }

    public async Task<string> GetUsersBasedOnStatus(UserSessionStatus status)
    {
        var users = UsersCache.Values
            .Where(s =>
                s.SessionState.Status == status &&
                DateTime.UtcNow - s.UpdatedAt <= TimeSpan.FromMinutes(15)
            )
            .ToList();
        
        var sb = new StringBuilder();

        if (users.Count == 0)
            return $"<b>Список пуст!</b>";

        switch (status)
        {
            case UserSessionStatus.FillingQuestions:
                sb.Append("<b>Сейчас заполняют документы:</b>\n\n");

                foreach (var user in users)
                {
                    sb.Append(user.ToString() + "\n");
                    sb.Append(user.DocumentState.ToString() + "\n");
                }

                return sb.ToString();

            case UserSessionStatus.UserSendingUsernameBlacklist:
                sb.Append("<b>Сейчас используют черный список:</b>\n\n");

                foreach (var user in users)
                {
                    sb.Append(user.ToString() + "\n");
                    sb.Append(user.BlockedPlayersState.ToString() + "\n");
                }

                break;

            case UserSessionStatus.SendingFirm:
                sb.Append("<b>Сейчас делают подписи:</b>\n\n");

                foreach (var user in users)
                {
                    sb.Append(user.ToString() + "\n");
                    sb.Append(user.FirmsState.ToString() + "\n");
                }

                break;
        }

        return sb.ToString();
    }

    public void UpdateSessionsTask()
    {
        _ = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    if (UsersCache is null || UsersCache.Count == 0)
                    {
                        await Task.Delay(Timers.SESSIONS_UPDATER_TIME);
                        continue;
                    }

                    Console.WriteLine("Обновление сессий началось.");

                    foreach (var user in UsersCache.Values)
                        await Sqlite.UpdateSessionFields(user.ChatId, user.LastMessage, user.UpdatedAt);

                    Console.WriteLine("Обновление сессий закончилось.");
                    await Task.Delay(Timers.SESSIONS_UPDATER_TIME);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in UpdateSessionsTask: {ex.Message}");
                }
            }
        });
    }
}

