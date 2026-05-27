using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using clrhost;

internal sealed partial class TelegramBot
{
    private readonly string _tokenBot;
    private readonly string _imageServiceApiToken;
    private readonly string _imageServiceUrl;
    private readonly string _serversServiceUrl;

    private readonly TelegramBotClient _bot;
    private readonly CancellationTokenSource _cts;
    private readonly KeyboardFactory _keyboardFactory;
    private readonly SessionManager _sessionManager;
    private readonly ServersAutoUpdateManager _serversUpdater;
    private readonly EventNotifier _eventNotifier;
    private readonly ReportCheckerService _reportCheckerTask;
    private readonly BlacklistTablesDownloader _blacklistTablesDownloader;
    private EventsDatabase _eventsDb;
    private ReportCheckerDatabase _rcDb;

    private readonly ConcurrentDictionary<long, SemaphoreSlim> _chatLocks;
    private readonly SemaphoreSlim _chatLocksDictSemaphore;

    private readonly BotCommand[] _botCommands = new[]
     {
                new BotCommand(command: "start", description: "Показать стартовое меню."),
                new BotCommand(command: "report", description: "Предложить идею или подать жалобу."),
                new BotCommand(command: "coder", description: "Кто написал бота.")
    };

    private readonly List<string> BlockedCommandsDuringOperations = new()
    {
        "report",
        "Документы",
        "Мониторинг серверов",
        "Подсказки",
        "События и мероприятия",
        "Подписаться",
        "Отписаться",
        "Показать подписанные",
        "Чёрный список",
        "Подписи",
        "Чекер жалоб (NEW)",
        "Добавить",
        "Аккаунты"
    };

    public TelegramBot(
       CancellationTokenSource cts,
       SessionManager sessionManager,
       string tokenBot,
       string imageServiceApiToken,
       string imageServiceUrl,
       string serversServiceUrl)
    {
        _cts = cts;
        _tokenBot = tokenBot;
        _imageServiceApiToken = imageServiceApiToken;
        _imageServiceUrl = imageServiceUrl;
        _serversServiceUrl = serversServiceUrl;

        _keyboardFactory = new KeyboardFactory();
        _bot = new TelegramBotClient(tokenBot);
        _chatLocks = new ConcurrentDictionary<long, SemaphoreSlim>();
        _chatLocksDictSemaphore = new SemaphoreSlim(1, 1);

        _eventsDb = new EventsDatabase();
        _rcDb = new ReportCheckerDatabase();
        _sessionManager = sessionManager;

        _serversUpdater = new ServersAutoUpdateManager(
            _bot,
            _cts,
            _keyboardFactory,
            _sessionManager,
            _serversServiceUrl);

        _blacklistTablesDownloader = new BlacklistTablesDownloader(_cts);
        _reportCheckerTask = new ReportCheckerService(_bot, _rcDb,_keyboardFactory);

        _eventNotifier = new EventNotifier(
            _eventsDb,
            _keyboardFactory,
            _bot);

        EventService.LoadEvents();
        StartBackgroundTasks();
    }

    private void StartBackgroundTasks()
    {
        _eventNotifier.StartNotifications();
        _serversUpdater.StartGlobalAutoUpdate();
        _blacklistTablesDownloader.StartDownloader();
        _sessionManager.UpdateSessionsTask();
        _reportCheckerTask.RunAsync();
    }

    private async Task<SemaphoreSlim> GetChatLockAsync(long chatId)
    {
        if (_chatLocks.TryGetValue(chatId, out var existingLock))
            return existingLock;

        await _chatLocksDictSemaphore.WaitAsync();
        try
        {
            if (_chatLocks.TryGetValue(chatId, out existingLock))
                return existingLock;

            var newLock = new SemaphoreSlim(1, 1);
            _chatLocks.TryAdd(chatId, newLock);
            return newLock;
        }
        finally
        {
            _chatLocksDictSemaphore.Release();
        }
    }

    public async Task InitAsync()
    {
        try
        {
            await IgnoreOldMessagesAsync();

            _bot.StartReceiving(errorHandler: HandleErrorAsync,
                               updateHandler: HandleUpdateAsync,
                               cancellationToken: _cts.Token,
                               receiverOptions: new Telegram.Bot.Polling.ReceiverOptions() { AllowedUpdates = Array.Empty<UpdateType>() });

            var currentCommands = await _bot.GetMyCommands();

            if (!CommandsAreEqual(currentCommands, _botCommands))
                await _bot.SetMyCommands(_botCommands);

            var me = _bot.GetMe();
        }
        catch { }
    }

    private bool CommandsAreEqual(BotCommand[] a, BotCommand[] b)
    {
        if (a.Length != b.Length)
            return false;

        for (int i = 0; i < a.Length; i++)
            if (a[i].Command != b[i].Command || a[i].Description != b[i].Description)
                return false;

        return true;
    }

    private async Task IgnoreOldMessagesAsync()
    {
        try
        {
            var oldUpdates = await _bot.GetUpdates();

            if (oldUpdates.Length > 0)
            {
                var lastUpdateId = oldUpdates.Last().Id + 1;
                await _bot.GetUpdates(offset: lastUpdateId);
            }
        }
        catch { }
    }

    private Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken token)
    {
        return Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken token)
    {
        if (update.Type != UpdateType.Message &&
            update.Type != UpdateType.CallbackQuery)
            return;

        if (update.Message == null &&
            update.CallbackQuery == null)
            return;

        if (update.Message?.From?.IsBot == true ||
            update.CallbackQuery?.From?.IsBot == true)
            return;

        var chatId = GetChatIdFromUpdate(update);

        if (chatId == default)
            return;

        var chatLock = await GetChatLockAsync(chatId);
        await chatLock.WaitAsync(token);
        try
        {
            var session = await _sessionManager.GetSessionAsync(chatId, update);
            await ProcessStateAsync(client, update, session);
        }
        finally
        {
            chatLock.Release();
        }
    }

    private long GetChatIdFromUpdate(Update update)
    {
        if (update.Message != null)
            return update.Message.Chat.Id;
        else if (update.CallbackQuery != null)
            return update.CallbackQuery.Message.Chat.Id;

        return default;
    }

    private async Task SendImageAsync(string imagePath, long chatId, string caption = "", InlineKeyboardMarkup ikm = null, ReplyKeyboardMarkup rkp = null)
    {
        if (chatId == default || !File.Exists(imagePath))
        {
            await SendMessageAsync("❌ <b>Произошла ошибка.</b>\nПожалуйста, попробуйте ещё раз.", chatId);
            return;
        }

        try
        {
            await using var stream = File.OpenRead(imagePath);
            var inputFile = InputFile.FromStream(stream);

            await _bot.SendPhoto(
                chatId: chatId,
                parseMode: ParseMode.Html,
                caption: caption,
                photo: inputFile,
                replyMarkup: ikm is null ? rkp : ikm,
                cancellationToken: _cts.Token
                );
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            await SendMessageAsync("❌ <b>Произошла ошибка.</b>\nПожалуйста, попробуйте ещё раз.", chatId);
        }
    }

    private async Task SendMessageAsync(string message, long chatId, InlineKeyboardMarkup ikm = null, ReplyKeyboardMarkup rkp = null)
    {
        if (chatId == default)
        {
            await SendMessageAsync("❌ <b>Произошла ошибка.</b>\nПожалуйста, попробуйте ещё раз.", chatId);
            return;
        }

        try
        {
            await _bot.SendMessage(
                chatId: chatId,
                text: message,
                replyMarkup: ikm is null ? rkp : ikm,
                parseMode: ParseMode.Html,
                cancellationToken: _cts.Token
                );
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private async Task SendInvalidFormatAsync(UserSession session)
    {
        await SendMessageAsync(
            "❌ Ответ имеет неверный формат. Повторите ввод: ",
            session.ChatId,
            null,
            _keyboardFactory.QuestionsMenu);
    }

    private async Task<bool> SavePhotoAsync(string fileId, string filePath)
    {
        try
        {
            var file = await _bot.GetFile(fileId, _cts.Token);

            await using var fs = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None
            );

            await _bot.DownloadFile(file.FilePath, fs, _cts.Token);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }
}
