using Telegram.Bot;
using Telegram.Bot.Types.Enums;

internal sealed class ServersAutoUpdateManager
{
    private readonly ITelegramBotClient _client;
    private readonly CancellationTokenSource _cts;
    private readonly string _serversServiceUrl;
    private readonly KeyboardFactory _keyboardFactory;
    private readonly SessionManager _sessionManager;

    private readonly Dictionary<long, string> _lastMessageCache = new();

    private Task _updateTask;
    private bool _isRunning = false;

    public ServersAutoUpdateManager(
        ITelegramBotClient client,
        CancellationTokenSource cts,
        KeyboardFactory keyboard,
        SessionManager sessionManager,
        string serversServiceUrl)
    {
        _client = client;
        _cts = cts;
        _serversServiceUrl = serversServiceUrl;
        _keyboardFactory = keyboard;
        _sessionManager = sessionManager;
    }


    public void StartGlobalAutoUpdate()
    {
        if (_isRunning)
            return;

        _isRunning = true;

        _updateTask = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(Timers.SERVERS_AUTO_UPDATE_TIME, _cts.Token);
                    await UpdateAllUserServers();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error in ServersAutoUpdateManager: {ex.Message}");
                }
            }

            _isRunning = false;
        }, _cts.Token);
    }


    private async Task UpdateAllUserServers()
    {
        var sessions = _sessionManager.UsersCache.Values.Where(u => u.ServersState.ServersUpdateEnabled).ToList();

        if (sessions.Count == 0)
            return;

        foreach (var session in sessions)
        {
            try
            {
                if (session.ServersState.ServerMessageId == 0 ||
                    string.IsNullOrWhiteSpace(session.ServersState.ServerType))
                    continue;

                var newMessage = await ServerService.GetServerInfoMessage(session.ServersState.ServerType, _serversServiceUrl);
                var hasChanged = false;

                lock (_lastMessageCache)
                {
                    if (!_lastMessageCache.TryGetValue(session.ChatId, out var cachedMessage) ||
                        cachedMessage != newMessage)
                    {
                        hasChanged = true;
                        _lastMessageCache[session.ChatId] = newMessage;
                    }
                }

                if (hasChanged)
                    await UpdateServerMessage(session, newMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при обновлении серверов {session.ChatId}: {ex.Message}");
            }
        }
    }


    private async Task UpdateServerMessage(UserSession session, string message)
    {
        try
        {
            await _client.EditMessageText(
                messageId: session.ServersState.ServerMessageId,
                chatId: session.ChatId,
                text: message,
                replyMarkup: _keyboardFactory.ServersUpdateMenu,
                parseMode: ParseMode.Html,
                cancellationToken: _cts.Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Не удалось обновить сообщение {session.ChatId}: {ex.Message}");
        }
    }

    public void RemoveUserCache(long chatId)
    {
        lock (_lastMessageCache)
        {
            _lastMessageCache.Remove(chatId);
        }
    }

    public async Task Stop()
    {
        if (!_isRunning)
            return;

        _cts.Cancel();

        if (_updateTask != null)
        {
            try
            {
                await _updateTask;
            }
            catch { }
        }
    }
}