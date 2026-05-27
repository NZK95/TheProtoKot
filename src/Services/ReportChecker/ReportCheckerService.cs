using Microsoft.Playwright;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

internal sealed class ReportCheckerService
{
    private readonly TelegramBotClient _bot;
    private readonly ReportCheckerDatabase _db;
    private readonly CancellationTokenSource _cts;
    private readonly KeyboardFactory _kb;
    private readonly SemaphoreSlim _checkSemaphore = new SemaphoreSlim(3, 3);
    private int _isChecking = 0;

    public ReportCheckerService(TelegramBotClient bot, ReportCheckerDatabase db, KeyboardFactory kb)
    {
        _bot = bot;
        _db = db;
        _cts = new CancellationTokenSource();
        _kb = kb;
    }

    public void RunAsync()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var playwright = await Playwright.CreateAsync();

                var browser = await playwright.Firefox.LaunchPersistentContextAsync(
                    userDataDir: "user-data-firefox",
                    new BrowserTypeLaunchPersistentContextOptions
                    {
                        Headless = true
                    });

                while (!_cts.IsCancellationRequested)
                {
                    if (Interlocked.CompareExchange(ref _isChecking, 1, 0) == 0)
                    {
                        try { await CheckReportsAsync(browser, _cts.Token); }
                        catch (Exception ex) { Console.WriteLine(ex.Message); }
                        finally { Interlocked.Exchange(ref _isChecking, 0); }
                    }

                    await Task.Delay(Timers.REPORT_CHECKER_UPDATE_TIME, _cts.Token);
                }

                await browser.CloseAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        });
    }

    public async Task CheckAccountNowAsync(UserSession session)
    {
        await _checkSemaphore.WaitAsync();

        var profileDir = $"user-data-firefox-check-{session.ChatId}";

        try
        {
            var chatID = session.ChatId;
            var nickname = session.ReportCheckerState.Nickname;
            var accountID = session.ReportCheckerState.AccountID;
            var server = $"{session.ReportCheckerState.Server[0].ToString().ToUpper()}{session.ReportCheckerState.Server.Substring(1).ToLower()}";
            var linksServer = ReportCheckerConstants.GetLinks(server);

            if (linksServer is null)
            {
                await SendNotificationAsync(chatID, $"❌ Неизвестный сервер: <code>{Escape(server)}</code>");
                return;
            }

            await _bot.EditMessageCaption(
                chatId: chatID,
                messageId: session.ReportCheckerState.ReportCheckerMessageId,
                replyMarkup: null,
                parseMode: ParseMode.Html,
                caption: $"🔍 Проверяем жалобы на <b>{Escape(nickname)}</b> (<code>{accountID}</code>) на сервере <b>{Escape(server)}</b>...");

            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Firefox.LaunchPersistentContextAsync(
                userDataDir: profileDir,
                new BrowserTypeLaunchPersistentContextOptions { Headless = true });

            var found = new List<(string text, string url, string videos)>();

            try
            {
                foreach (var link in linksServer)
                {
                    var page = await browser.NewPageAsync();
                    try
                    {
                        await page.GotoAsync(link);
                        await page.WaitForSelectorAsync("a.body", new() { Timeout = 10000 });

                        var topics = page.Locator("a.body");
                        int count = await topics.CountAsync();

                        for (int i = 0; i < count; i++)
                        {
                            var topic = topics.Nth(i);

                            string playerName = await TryGetInnerTextAsync(
                                topic.Locator(".main__title > span").First);

                            bool nicknameMatch = playerName.Equals(nickname, StringComparison.OrdinalIgnoreCase);
                            bool idMatch = playerName.Equals(accountID.ToString(), StringComparison.Ordinal);

                            if (!nicknameMatch && !idMatch)
                                continue;

                            string url = await TryGetAttributeAsync(topic, "href");
                            string videos = "";

                            if (!string.IsNullOrEmpty(url))
                            {
                                try
                                {
                                    var complaintPage = await browser.NewPageAsync();
                                    await complaintPage.GotoAsync($"https://amazing-online.com{url}");
                                    await complaintPage.WaitForSelectorAsync(".body__text", new() { Timeout = 5000 });

                                    var links = await complaintPage.Locator(".body__attachment-items a[href]").AllAsync();
                                    List<string> videoLinks = new();

                                    foreach (var l in links)
                                    {
                                        string href = await TryGetAttributeAsync(l, "href");
                                        if (!string.IsNullOrWhiteSpace(href))
                                            videoLinks.Add(href);
                                    }

                                    if (videoLinks.Count > 0)
                                        videos = string.Join("\n", videoLinks);

                                    await complaintPage.CloseAsync();
                                }
                                catch { }
                            }

                            string status = await TryGetInnerTextAsync(topic.Locator(".main__title-label span"));
                            string author = await TryGetInnerTextAsync(topic.Locator(".main__title-author-name"));
                            string dateTime = await TryGetInnerTextAsync(topic.Locator(".main__title-author-date"));
                            string lastAuthor = await TryGetInnerTextAsync(topic.Locator(".latest__item.item_author"));
                            string lastTime = await TryGetInnerTextAsync(topic.Locator(".latest__item:not(.item_author)"));
                            string section = GetSectionName(link);

                            found.Add((BuildMessage(playerName, status, author, dateTime, lastAuthor, lastTime, url, server, section, videos, false), url, videos));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CheckAccountNow] Ошибка при обработке {link}: {ex.Message}");
                    }
                    finally
                    {
                        await page.CloseAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CheckAccountNow] Ошибка: {ex.Message}");
                await SendNotificationAsync(chatID, "❌ Ошибка при проверке жалоб.");
                return;
            }
            finally
            {
                await browser.CloseAsync();

                try
                {
                    if (Directory.Exists(profileDir))
                        Directory.Delete(profileDir, recursive: true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CheckAccountNow] Не удалось удалить профиль {profileDir}: {ex.Message}");
                }
            }

            if (found.Count == 0)
            {
                await _bot.EditMessageCaption(
                    chatId: chatID,
                    messageId: session.ReportCheckerState.ReportCheckerMessageId,
                    replyMarkup: new InlineKeyboardMarkup(
                        InlineKeyboardButton.WithCallbackData("Назад", "ReportChecker@BackToChooseWhoCheck")),
                    parseMode: ParseMode.Html,
                    caption: $"✅ Жалоб на <b>{Escape(nickname)}</b> (<code>{accountID}</code>) не найдено.");

                return;
            }

            session.ReportCheckerState.FoundReports = found;
            session.ReportCheckerState.CurrentReportPage = 0;

            await _bot.EditMessageCaption(
                 chatId: chatID,
                 messageId: session.ReportCheckerState.ReportCheckerMessageId,
                 parseMode: ParseMode.Html,
                 replyMarkup: new InlineKeyboardMarkup(
                        InlineKeyboardButton.WithCallbackData("Назад", "ReportChecker@BackToChooseWhoCheck")),
                caption: $"🔎 Найдено {found.Count} {AlignmentMapper.GetComplaintWord(found.Count)} на <b>{Escape(nickname)}</b> (<code>{accountID}</code>).");

            await ShowReportPageAsync(session, true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            _checkSemaphore.Release();
        }
    }

    public async Task ShowReportPageAsync(UserSession session, bool firstMessage = false)
    {
        var reports = session.ReportCheckerState.FoundReports;
        var page = session.ReportCheckerState.CurrentReportPage;
        var total = reports.Count;

        var (text, url, videos) = reports[page];

        var buttons = new List<InlineKeyboardButton[]>();
        var navRow = new List<InlineKeyboardButton>();

        if (page > 0)
            navRow.Add(InlineKeyboardButton.WithCallbackData("◀️", "ReportChecker@PagePrev"));

        if (page < total - 1)
            navRow.Add(InlineKeyboardButton.WithCallbackData("▶️", "ReportChecker@PageNext"));

        buttons.Add(navRow.ToArray());

        if (!string.IsNullOrEmpty(url))
            buttons.Add(new[] { InlineKeyboardButton.WithUrl("👁 Посмотреть жалобу", $"https://amazing-online.com{url}") });

        if (!string.IsNullOrEmpty(videos))
        {
            var videoLinks = videos.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < videoLinks.Length; i++)
                buttons.Add(new[] { InlineKeyboardButton.WithUrl($"🎥 Видео {i + 1}", videoLinks[i]) });
        }

        var keyboard = new InlineKeyboardMarkup(buttons);

        if (firstMessage)
        {
            var msg = await _bot.SendMessage(
                chatId: session.ChatId,
                text: text,
                replyMarkup: keyboard,
                parseMode: ParseMode.Html);

            session.ReportCheckerState.PageMessageId = msg.MessageId;
        }
        else
        {
            await _bot.EditMessageText(
                chatId: session.ChatId,
                messageId: session.ReportCheckerState.PageMessageId,
                text: text,
                replyMarkup: keyboard,
                parseMode: ParseMode.Html);
        }
    }

    private async Task CheckReportsAsync(IBrowserContext browser, CancellationToken cancellationToken)
    {
        var accounts = await _db.GetAllAccountsAsync();

        if (accounts.Count == 0)
            return;

        var byServer = accounts.GroupBy(a => a.Server);

        foreach (var serverGroup in byServer)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            string server = $"{serverGroup.Key[0].ToString().ToUpper()}{serverGroup.Key.Substring(1).ToLower()}";
            var linksServer = ReportCheckerConstants.GetLinks(server);

            if (linksServer is null)
                continue;

            foreach (var link in linksServer)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var page = await browser.NewPageAsync();

                try
                {
                    await page.GotoAsync(link);
                    await page.WaitForSelectorAsync("a.body", new() { Timeout = 10000 });

                    var topics = page.Locator("a.body");
                    int count = await topics.CountAsync();

                    for (int i = 0; i < count; i++)
                    {
                        var topic = topics.Nth(i);

                        string playerName = await TryGetInnerTextAsync(
                            topic.Locator(".main__title > span").First);

                        var matchedAccounts = serverGroup.Where(a =>
                            playerName.Equals(a.Nickname, StringComparison.OrdinalIgnoreCase) ||
                            playerName.Equals(a.AccountId.ToString(), StringComparison.Ordinal)).ToList();

                        if (matchedAccounts.Count == 0)
                            continue;

                        string url = await TryGetAttributeAsync(topic, "href");

                        if (await _db.IsComplaintSentAsync(url))
                            continue;

                        string status = await TryGetInnerTextAsync(topic.Locator(".main__title-label span"));

                        if (status.Equals("Проверено", StringComparison.OrdinalIgnoreCase))
                            continue;

                        string videos = "";

                        if (!string.IsNullOrEmpty(url))
                        {
                            try
                            {
                                var complaintPage = await browser.NewPageAsync();
                                await complaintPage.GotoAsync($"https://amazing-online.com{url}");
                                await complaintPage.WaitForSelectorAsync(".body__text", new() { Timeout = 5000 });

                                var links = await complaintPage.Locator(".body__attachment-items a[href]").AllAsync();
                                List<string> videoLinks = new();

                                foreach (var l in links)
                                {
                                    string href = await TryGetAttributeAsync(l, "href");
                                    if (!string.IsNullOrWhiteSpace(href))
                                        videoLinks.Add(href);
                                }

                                if (videoLinks.Count > 0)
                                    videos = string.Join("\n", videoLinks);

                                await complaintPage.CloseAsync();
                            }
                            catch { }
                        }

                        string dateTime = await TryGetInnerTextAsync(topic.Locator(".main__title-author-date"));
                        string author = await TryGetInnerTextAsync(topic.Locator(".main__title-author-name"));
                        string lastAuthor = await TryGetInnerTextAsync(topic.Locator(".latest__item.item_author"));
                        string lastTime = await TryGetInnerTextAsync(topic.Locator(".latest__item:not(.item_author)"));
                        string section = GetSectionName(link);
                        string message = BuildMessage(playerName, status, author, dateTime, lastAuthor, lastTime, url, server, section, videos);

                        foreach (var account in matchedAccounts)
                        {
                            if (!account.NotificationsEnabled)
                                continue;

                            await SendNotificationAsync(account.ChatId, message, BuildKeyboard(url, videos));
                        }

                        await _db.MarkComplaintSentAsync(url);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ReportChecker] Ошибка при обработке {link}: {ex.Message}");
                }
                finally
                {
                    await page.CloseAsync();
                }
            }
        }
    }

    private static InlineKeyboardMarkup? BuildKeyboard(string url, string videos)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        if (!string.IsNullOrEmpty(url))
            buttons.Add(new[] { InlineKeyboardButton.WithUrl("👁 Посмотреть жалобу", $"https://amazing-online.com{url}") });

        if (!string.IsNullOrEmpty(videos))
        {
            var videoLinks = videos.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < videoLinks.Length; i++)
                buttons.Add(new[] { InlineKeyboardButton.WithUrl($"🎥 Видео №{i + 1}", videoLinks[i]) });
        }

        return buttons.Count > 0 ? new InlineKeyboardMarkup(buttons) : null;
    }

    private async Task SendNotificationAsync(long chatId, string message, InlineKeyboardMarkup? replyMarkup = null)
    {
        try
        {
            await _bot.SendMessage(
                chatId: chatId,
                text: message,
                parseMode: ParseMode.Html,
                replyMarkup: replyMarkup);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ReportChecker] Не удалось отправить сообщение chatId={chatId}: {ex.Message}");
        }
    }

    private static string BuildMessage(
        string playerName,
        string status,
        string author,
        string dateTime,
        string lastAuthor,
        string lastTime,
        string url,
        string server,
        string section,
        string videos,
        bool mainTitle = true)
    {
        if (!string.IsNullOrEmpty(status))
            status = char.ToUpper(status[0]) + status.Substring(1).ToLower();

        var sb = new System.Text.StringBuilder();

        if (mainTitle)
        {
            sb.AppendLine($"🔔 На аккаунт <b>{Escape(playerName)}</b> на сервере <b>{Escape(server)}</b> поступила жалоба!");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine($"🚨 Найдена <a href=\"https://amazing-online.com{url}\">жалоба</a> на <b>{Escape(playerName)}</b> на сервере <b>{Escape(server)}</b>!");
            sb.AppendLine();
        }

        sb.AppendLine($"<b>Раздел:</b> {Escape(section)}");
        sb.AppendLine($"<b>Автор жалобы:</b> {Escape(author)}");
        sb.AppendLine($"<b>Статус:</b> {Escape(status)}");
        sb.AppendLine($"<b>Дата:</b> {Escape(dateTime)}");
        sb.AppendLine($"<b>Последний ответ:</b> {Escape(lastAuthor)} ({Escape(lastTime)})");

        return sb.ToString();
    }

    private static string Escape(string text) =>
        System.Net.WebUtility.HtmlEncode(text);

    private static string GetSectionName(string url)
    {
        var number = int.Parse(url.Split('/').Last());

        return number switch
        {
            1 or 8 or 15 or 22 or 29 or 36 or 43 or 54 or 61 or 68 or 75 or 82 => "Жалобы на администрацию",
            2 or 9 or 16 or 23 or 30 or 37 or 44 or 55 or 62 or 69 or 76 or 83 => "Жалобы на лидеров и заместителей",
            3 or 10 or 17 or 24 or 31 or 38 or 45 or 56 or 63 or 70 or 77 or 84 => "Жалобы на игроков, не состоящих в организациях",
            4 or 11 or 18 or 25 or 32 or 39 or 46 or 57 or 64 or 71 or 78 or 85 => "Жалобы на игроков, состоящих в нелег. организациях",
            5 or 12 or 19 or 26 or 33 or 40 or 47 or 58 or 65 or 72 or 79 or 86 => "Жалобы на игроков, состоящих в гос. организациях",
            6 or 13 or 20 or 27 or 34 or 41 or 48 or 59 or 66 or 73 or 80 or 87 => "Запросы на опровержение каптов/бизваров",
            _ => "Неизвестный раздел"
        };
    }

    private static async Task<string> TryGetInnerTextAsync(ILocator locator)
    {
        try { return await locator.InnerTextAsync(); }
        catch { return string.Empty; }
    }

    private static async Task<string> TryGetAttributeAsync(ILocator locator, string attr)
    {
        try { return await locator.GetAttributeAsync(attr) ?? string.Empty; }
        catch { return string.Empty; }
    }
}