using AmazingBot;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

internal sealed partial class TelegramBot
{
    private async Task HandleCallbackQueryAsync(UserSession session, Update update, ITelegramBotClient client)
    {
        try
        {
            var callbackData = update.CallbackQuery.Data;
            var prefix = callbackData.Split('@')[0];
            var data = callbackData.Split('@')[1];

            switch (prefix)
            {
                case "Admin":
                    await ElaborateAdminPanelAsync(session, update, client, data);
                    break;

                case "MainMenu":
                    await client.AnswerCallbackQuery(update.CallbackQuery.Id);
                    await DeleteMessagesBeforeMainMenu(session);
                    await HandleUserMessageAsync("/start", update, session, client);
                    break;

                case "EventsMenu":
                    await ElaborateBackToMenuFromEventsAsync(session, update, client);
                    break;

                case "BlacklistMenu":
                    await ElaborateBlacklistMenuAsync(session, update, client);
                    break;

                case "ServersHintsMenu":
                    await ElaborateCallbackBackToMenuFromHintsAsync(session, update, client);
                    break;

                case "FirmsMenu":
                    await ElaborateCallbackBackToMenuFromFirmsAsync(session, update, client);
                    break;

                case "Servers":
                    await HandleCallbackServersAsync(session, update, client, data);
                    break;

                case "BotCommands":
                    await HandleCallbackBotCommandsAsync(session, update, client, data);
                    break;

                case "Fractions":
                    await HandleCallbackFractionsAsync(session, update, client, data);
                    break;

                case "DocumentType":
                    await HandleCallbackDocumentTypeAsync(session, update, client, data);
                    break;

                case "Hints":
                    await HandleCallbackHintsAsync(session, data, client, update);
                    break;

                case "HintsColorBackground":
                    await HandleCallbackHintsColorBackgroundAsync(session, data, client, update);
                    break;

                case "Servers-Hints":
                    await HandleCallbackServerHintsAsync(session, update, client, data);
                    break;

                case "Servers-RC":
                    await ElaborateAddReportedAccountAsync(session, data);
                    break;

                case "Servers-Blocked":
                    await ElaborateCallbackBlacklistAsync(session, update, client, data);
                    break;

                case "ServersUpdate":
                    await HandleCallbackStopServersAutoupdateAsync(session, update, client, data);
                    break;

                case "BackToMainMenu":
                    await HandleCallbackBackToMainMenuAsync(session, update, client, data);
                    break;

                case "ResultFormat":
                    await HandleCallbackResultFormatAsync(session, update, client, data);
                    break;

                case "Event":
                    await HandleCallbackAddEventAsync(session, update, client, data);
                    break;

                case "EventRemove":
                    await ElaborateCallbackRemoveEventAsync(session, update, client, data);
                    break;

                case "Stamp":
                    await ElaboratCallbackStampAsync(session, update, client, data);
                    break;

                case "DocumentMenu":
                    await ElaborateDocumentMenuAsync(session, data, update, client);
                    break;

                case "CheckAccount":
                    await ElaborateCheckAccountForReportsAsync(session, update, client, data);
                    break;

                case "ReportChecker":
                    {
                        await client.AnswerCallbackQuery(update.CallbackQuery.Id);

                        switch (data)
                        {
                            case "Accounts":
                                {
                                    var accounts = await _rcDb.GetAccountsAsync(session.ChatId);
                                    int limit = await _rcDb.GetAccountLimitAsync(session.ChatId);
                                    int count = await _rcDb.GetAccountsCountAsync(session.ChatId);

                                    await _bot.EditMessageCaption(
                                     chatId: session.ChatId,
                                     messageId: session.ReportCheckerState.ReportCheckerMessageId,
                                     replyMarkup: _keyboardFactory.BuildListOfTrackedForReportsPlayersShowPropertiesMenu(accounts, count >= limit ? false : true, true),
                                     parseMode: ParseMode.Html,
                                     caption: "👥 <b>Ваши аккаунты:</b>");

                                    break;
                                }

                            case "Check":
                                {
                                    var accounts = await _rcDb.GetAccountsAsync(session.ChatId);

                                    await _bot.EditMessageCaption(
                                    chatId: session.ChatId,
                                    messageId: session.ReportCheckerState.ReportCheckerMessageId,
                                    replyMarkup: _keyboardFactory.BuildListOfTrackedForReportsPlayersCheckNowMenu(accounts),
                                    caption: "👇 Выберите аккаунт, который хотите проверить:",
                                    parseMode: ParseMode.Html);

                                    break;
                                }

                            case "BackFromAccounts":
                                {
                                    var accounts = await _rcDb.GetAccountsAsync(session.ChatId);
                                    int limit = await _rcDb.GetAccountLimitAsync(session.ChatId);
                                    int count = await _rcDb.GetAccountsCountAsync(session.ChatId);

                                    await _bot.EditMessageCaption(
                                    chatId: session.ChatId,
                                    messageId: session.ReportCheckerState.ReportCheckerMessageId,
                                    replyMarkup: _keyboardFactory.ReportCheckerMenu,
                                    parseMode: ParseMode.Html,
                                    caption: Messages.ReportCheckerMessage);

                                    break;
                                }

                            case "BackToMainMenu":
                                {
                                    if (session.ReportCheckerState.ReportCheckerMessageId != 0)
                                    {
                                        try
                                        {
                                            await _bot.DeleteMessage(chatId: session.ChatId, messageId: session.ReportCheckerState.ReportCheckerMessageId);
                                        }
                                        catch { }
                                    }

                                    await HandleUserMessageAsync("/start", update, session, client);
                                    break;
                                }

                            case "BackToChooseWhoCheck":
                                {
                                    var accounts = await _rcDb.GetAccountsAsync(session.ChatId);

                                    await _bot.EditMessageCaption(
                                   chatId: session.ChatId,
                                   messageId: session.ReportCheckerState.ReportCheckerMessageId,
                                   replyMarkup: _keyboardFactory.BuildListOfTrackedForReportsPlayersCheckNowMenu(accounts),
                                   caption: "👇 Выберите аккаунт, который хотите проверить:",
                                   parseMode: ParseMode.Html);

                                    break;
                                }

                            case "PageNext":
                                session.ReportCheckerState.CurrentReportPage++;
                                await _reportCheckerTask.ShowReportPageAsync(session);
                                break;

                            case "PagePrev":
                                session.ReportCheckerState.CurrentReportPage--;
                                await _reportCheckerTask.ShowReportPageAsync(session);
                                break;
                        }

                        break;
                    }

                case "AccountMenuBack":
                    {
                        await client.AnswerCallbackQuery(update.CallbackQuery.Id);

                        var accounts = await _rcDb.GetAccountsAsync(session.ChatId);
                        int limit = await _rcDb.GetAccountLimitAsync(session.ChatId);
                        int count = await _rcDb.GetAccountsCountAsync(session.ChatId);

                        await _bot.EditMessageCaption(
                  messageId: session.ReportCheckerState.ReportCheckerMessageId,
                  chatId: session.ChatId,
                  caption: "👥 Ваши аккаунты:",
                  parseMode: ParseMode.Html,
                  replyMarkup: _keyboardFactory.BuildListOfTrackedForReportsPlayersShowPropertiesMenu(accounts, count >= limit ? false : true, true));

                        break;
                    }


                case "CheckedAccountProperties":
                    await ElaborateShowPropertiesOfTrackedAccountAsync(session, update, client, data);
                    break;

                case "AccountMenuNotifications":
                    await ElaborateNotificationsForAccountCheckedForReportsAsync(session, update, client, data);
                    break;

                case "AccountMenuLogout":
                    await ElaborateLogoutFromCheckedForReportsAccountAsync(session, update, client, data);
                    break;

                case "AddCheckedAccount":
                    await ElaborateAddAccountToCheckForReportsAsync(session, update, client);
                    break;

                case "prev":
                    await ElaboratePreviousPageForBlockedPlayersListAsync(session, update, client, data);
                    break;

                case "next":
                    await ElaborateNextPageForBlockedPlayersListAsync(session, update, client, data);
                    break;
            }
        }
        catch
        {
            await SendMessageAsync("❌ <b>Произошла ошибка.</b>\nПожалуйста, попробуйте ещё раз.", session.ChatId);
        }
    }

    private async Task ElaborateShowPropertiesOfTrackedAccountAsync(UserSession session, Update update, ITelegramBotClient client, string data)
    {
        await client.AnswerCallbackQuery(update.CallbackQuery.Id);

        var accountId = long.Parse(data.Split(',')[0]);
        var nickname = data.Split(',')[1];
        var server = data.Split(',')[2];
        var notifications = bool.Parse(data.Split(',')[3]);

        var account = new ReportedAccountRecord
        {
            AccountId = accountId,
            ChatId = session.ChatId,
            Server = server,
            Username = _sessionManager.GetUsernameByChatId(session.ChatId),
            Nickname = nickname,
            NotificationsEnabled = notifications
        };

        var message =
           $"👤 <b>Ник:</b> {nickname}\n" +
           $"🆔 <b>ID:</b> {accountId}\n" +
           $"🌍 <b>Сервер:</b> {server}\n" +
           $"🔔 <b>Уведомления:</b> {(notifications ? "включены" : "выключены")}";

        await _bot.EditMessageCaption(
            messageId: session.ReportCheckerState.ReportCheckerMessageId,
            chatId: session.ChatId,
            caption: message,
            parseMode: ParseMode.Html,
            replyMarkup: _keyboardFactory.BuildCheckedForReportsAccountMenu(account));
    }

    private async Task ElaborateCheckAccountForReportsAsync(UserSession session, Update update, ITelegramBotClient client, string data)
    {
        await client.AnswerCallbackQuery(update.CallbackQuery.Id);

        session.ReportCheckerState.AccountID = long.Parse(data.Split(',')[0]);
        session.ReportCheckerState.Nickname = data.Split(',')[1];
        session.ReportCheckerState.Server = data.Split(',')[2];

        await _reportCheckerTask.CheckAccountNowAsync(session);
    }

    private async Task ElaborateLogoutFromCheckedForReportsAccountAsync(UserSession session, Update update, ITelegramBotClient client, string data)
    {
        await client.AnswerCallbackQuery(update.CallbackQuery.Id);

        var accountId = long.Parse(data.Split(',')[0]);
        var nickname = data.Split(',')[1];

        await _rcDb.DeleteAccountAsync(session.ChatId, accountId, nickname);

        await _bot.EditMessageCaption(
      messageId: session.ReportCheckerState.ReportCheckerMessageId,
      chatId: session.ChatId,
      caption: $"✅ Вы вышли из <b>{nickname}</b> (<code>{accountId}</code>)",
      parseMode: ParseMode.Html,
      replyMarkup: _keyboardFactory.BuildCheckedForReportsAccountMenu(null!, true));
    }

    private async Task ElaborateNotificationsForAccountCheckedForReportsAsync(UserSession session, Update update, ITelegramBotClient client, string data)
    {
        await client.AnswerCallbackQuery(update.CallbackQuery.Id);

        var parts = data.Split(',');
        var accountId = long.Parse(parts[0]);
        var nickname = parts[1];
        var server = parts[2];
        var notifications = bool.Parse(parts[3]);

        await _rcDb.ToggleNotificationsAsync(session.ChatId, accountId);

        string statusText = notifications
            ? $"🔕 Уведомления для <b>{nickname}</b> (<code>{accountId}</code>) <b>выключены</b>"
            : $"🔔 Уведомления для <b>{nickname}</b> (<code>{accountId}</code>) <b>включены</b>";

        await _bot.EditMessageCaption(
            messageId: session.ReportCheckerState.ReportCheckerMessageId,
            chatId: session.ChatId,
            caption: statusText,
            parseMode: ParseMode.Html,
            replyMarkup: _keyboardFactory.BuildCheckedForReportsAccountMenu(null!, true));
    }

    private async Task<bool> ElaborateAddAccountToCheckForReportsAsync(UserSession session, Update update, ITelegramBotClient client)
    {
        await client.AnswerCallbackQuery(update.CallbackQuery.Id);

        int limit = await _rcDb.GetAccountLimitAsync(session.ChatId);
        int count = await _rcDb.GetAccountsCountAsync(session.ChatId);

        if (count >= limit)
        {
            await SendMessageAsync($"❗ Достигнут лимит аккаунтов ({limit}).", session.ChatId);
            return false;
        }

        await _bot.EditMessageCaption(
        messageId: session.ReportCheckerState.ReportCheckerMessageId,
        chatId: session.ChatId,
        parseMode: ParseMode.Html,
        caption: "✏️ 1) Введите игровой никнейм в формате <b>Имя_Фамилия</b> на английском (или /cancel для отмены):");

        session.SessionState.Status = UserSessionStatus.UserFillingDataReportChecker;
        return true;
    }

    private async Task ElaborateDocumentMenuAsync(UserSession session, string data, Update update, ITelegramBotClient client)
    {
        await client.AnswerCallbackQuery(update.CallbackQuery.Id);

        switch (data)
        {
            case "Firms":
                {
                    if (session.LastCommandMessageId != 0)
                    {
                        try
                        {
                            await _bot.DeleteMessage(chatId: session.ChatId, messageId: session.LastCommandMessageId);
                        }
                        catch { }
                    }

                    if (session.FirmsState.LastFirmsMessageId != 0)
                    {
                        try
                        {
                            await _bot.DeleteMessage(chatId: session.ChatId, messageId: session.FirmsState.LastFirmsMessageId);
                        }
                        catch { }
                    }

                    await using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "assets", "firms.jpg"));
                    var inputFile = InputFile.FromStream(stream);

                    session.FirmsState.LastFirmsMessageId = session.LastCommandMessageId = (await _bot.SendPhoto(
                    chatId: session.ChatId,
                    photo: inputFile,
                    replyMarkup: _keyboardFactory.BuildStamps(),
                    caption: "Выберите нужный штамп.",
                    parseMode: ParseMode.Html)).MessageId;
                    break;
                }

            case "Documents":
                {
                    if (session.LastCommandMessageId != 0)
                    {
                        try
                        {
                            await _bot.DeleteMessage(chatId: session.ChatId, messageId: session.LastCommandMessageId);
                        }
                        catch { }
                    }

                    await using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "assets", "create-document.jpg"));
                    var inputFile = InputFile.FromStream(stream);
                    session.DocumentState.LastQuestionMessageId = session.LastCommandMessageId = (await _bot.SendPhoto(
                       chatId: session.ChatId,
                       photo: inputFile,
                       replyMarkup: _keyboardFactory.Fractions,
                       caption: "Выберите фракцию.",
                       parseMode: ParseMode.Html)).MessageId;

                    break;
                }

            case "BackToMainMenu":
                {
                    await HandleUserMessageAsync("/start", update, session, client);
                    break;
                }
        }
    }

    private async Task ElaboratePreviousPageForBlockedPlayersListAsync(UserSession session, Update update, ITelegramBotClient client, string data)
    {
        await client.AnswerCallbackQuery(update.CallbackQuery.Id);

        if (session.BlockedPlayersState.CurrentPage != 0)
            session.BlockedPlayersState.CurrentPage--;

        await _bot.EditMessageCaption(
chatId: session.ChatId,
messageId: session.BlockedPlayersState.LastBlockedPlayersMessageId,
caption: session.BlockedPlayersState.Pages[int.Parse(data)],
parseMode: ParseMode.Html,
replyMarkup: _keyboardFactory.BuildPagesMenu(session.BlockedPlayersState.CurrentPage, session.BlockedPlayersState.Pages.Count));
    }

    private async Task ElaborateNextPageForBlockedPlayersListAsync(UserSession session, Update update, ITelegramBotClient client, string data)
    {
        await client.AnswerCallbackQuery(update.CallbackQuery.Id);

        if (session.BlockedPlayersState.CurrentPage < session.BlockedPlayersState.Pages.Count - 1)
            session.BlockedPlayersState.CurrentPage++;

        await _bot.EditMessageCaption(
chatId: session.ChatId,
messageId: session.BlockedPlayersState.LastBlockedPlayersMessageId,
caption: session.BlockedPlayersState.Pages[int.Parse(data)],
parseMode: ParseMode.Html,
replyMarkup: _keyboardFactory.BuildPagesMenu(session.BlockedPlayersState.CurrentPage, session.BlockedPlayersState.Pages.Count));
    }

    private async Task ElaborateAddReportedAccountAsync(UserSession session, string data)
    {
        session.ReportCheckerState.Server = data;

        var account = new ReportedAccountRecord()
        {
            ChatId = session.ChatId,
            Username = session.TelegramUsername ?? "N/A",
            Server = session.ReportCheckerState.Server,
            AccountId = session.ReportCheckerState.AccountID,
            Nickname = session.ReportCheckerState.Nickname,
            NotificationsEnabled = true
        };

        await _rcDb.AddAccountAsync(account);
        await _rcDb.AddAccountWithLimitsAsync(session.ChatId);

        var currentAccountsCount = await _rcDb.GetAccountsCountAsync(session.ChatId);
        var accountsLimit = await _rcDb.GetAccountLimitAsync(session.ChatId);
        var remaining = accountsLimit - currentAccountsCount;

        string slotsInfo;
        if (remaining <= 0)
            slotsInfo = $"📊 Использовано: {currentAccountsCount}/{accountsLimit}\n⛔ Лимит достигнут — добавить новые аккаунты невозможно.";
        else if (remaining == 1)
            slotsInfo = $"📊 Использовано: {currentAccountsCount}/{accountsLimit}\n⚠️ Остался последний слот!";
        else
            slotsInfo = $"📊 Использовано: {currentAccountsCount}/{accountsLimit}\n🟢 Осталось слотов: {remaining}";

        var message =
            $"✅ <b>Аккаунт успешно добавлен!</b>\n\n" +
            $"👤 <b>Ник:</b> {session.ReportCheckerState.Nickname}\n" +
            $"🆔 <b>ID:</b> {session.ReportCheckerState.AccountID}\n" +
            $"🌍 <b>Сервер:</b> {session.ReportCheckerState.Server}\n" +
            $"🔔 <b>Уведомления:</b> включены\n\n" +
            slotsInfo;

        await _bot.EditMessageCaption(
   chatId: session.ChatId,
   messageId: session.ReportCheckerState.ReportCheckerMessageId,
   replyMarkup: _keyboardFactory.BuildCheckedForReportsAccountMenu(null!, true),
   parseMode: ParseMode.Html,
   caption: message);

        session.ReportCheckerState.Reset();
    }

    private async Task ElaborateBlacklistMenuAsync(UserSession session, Update update, ITelegramBotClient client)
    {
        if (session.BlockedPlayersState.LastBlockedPlayersMessageId != 0)
        {
            try
            {
                await _bot.DeleteMessage(session.ChatId, session.BlockedPlayersState.LastBlockedPlayersMessageId);
            }
            catch { }
        }

        session.SessionState.Status = UserSessionStatus.None;
        await HandleUserMessageAsync("/start", update, session, client);
    }

    private async Task<bool> ElaborateAdminPanelAsync(UserSession session, Update update, ITelegramBotClient client, string data)
    {
        await client.AnswerCallbackQuery(update.CallbackQuery.Id);

        if (!TelegramConstants.Admins.Contains(session.ChatId))
            return true;

        switch (data)
        {
            case "Limits":
                await SendMessageAsync("Введите <code>ChatId</code> участника и количество лимитов в формате <code>ChatId-Limits</code> (или /cancel для отмены).", session.ChatId);
                session.SessionState.Status = UserSessionStatus.AdminWaitingLimitsUsername;
                break;

            case "Kick":
                await SendMessageAsync("Введите юзернейм участника под кик (или /cancel для отмены). ", session.ChatId);
                session.SessionState.Status = UserSessionStatus.AdminWaitingKickUsername;
                break;

            case "Ban":
                await SendMessageAsync("Введите юзернейм участника под изгнание (или /cancel для отмены).", session.ChatId);
                session.SessionState.Status = UserSessionStatus.AdminWaitingBanUsername;
                break;

            case "Users5m":
                {
                    var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);

                    var sessionsLast5Minutes = _sessionManager.UsersCache.Values
                        .Where(s => s.UpdatedAt >= fiveMinutesAgo)
                        .ToList();

                    var fileName = $"{Guid.NewGuid()}.txt";
                    var path = Path.Combine(PathService.ADMIN_PATH, fileName);

                    var sb = new StringBuilder();

                    sb.AppendLine("{");
                    sb.AppendLine($"  \"Count\": {sessionsLast5Minutes.Count},");
                    sb.AppendLine("  \"Users\": [");

                    for (int i = 0; i < sessionsLast5Minutes.Count; i++)
                    {
                        var user = sessionsLast5Minutes[i];

                        sb.AppendLine("    {");
                        sb.AppendLine($"      \"Username\": \"{AlignmentMapper.Escape(user.TelegramUsername)}\",");
                        sb.AppendLine($"      \"ChatId\": {user.ChatId},");
                        sb.AppendLine($"      \"CreatedAt\": \"{user.CreatedAt:yyyy-MM-dd HH:mm:ss}\",");
                        sb.AppendLine($"      \"UpdatedAt\": \"{user.UpdatedAt:yyyy-MM-dd HH:mm:ss}\",");
                        sb.AppendLine($"      \"LastMessage\": \"{AlignmentMapper.Escape(user.LastMessage)}\"");
                        sb.Append("    }");

                        if (i < sessionsLast5Minutes.Count - 1)
                            sb.AppendLine(",");
                        else
                            sb.AppendLine();
                    }

                    sb.AppendLine("  ]");
                    sb.AppendLine("}");

                    await File.WriteAllTextAsync(path, sb.ToString());

                    await using var stream = File.OpenRead(path);

                    await _bot.SendDocument(
                        chatId: session.ChatId,
                        document: InputFile.FromStream(stream, "Пользователи-5-минут.txt"),
                        caption: "Вот ваш файл"
                    );

                    File.Delete(path);
                    break;
                }

            case "UsersDay":
                {
                    var oneDayAgo = DateTime.UtcNow.AddDays(-1);

                    var sessionsLastDay = _sessionManager.UsersCache.Values
                        .Where(s => s.UpdatedAt >= oneDayAgo)
                        .ToList();

                    var fileName = $"{Guid.NewGuid()}.txt";
                    var path = Path.Combine(PathService.ADMIN_PATH, fileName);

                    var sb = new StringBuilder();

                    sb.AppendLine("{");
                    sb.AppendLine($"  \"Count\": {sessionsLastDay.Count},");
                    sb.AppendLine("  \"Users\": [");

                    for (int i = 0; i < sessionsLastDay.Count; i++)
                    {
                        var user = sessionsLastDay[i];

                        sb.AppendLine("    {");
                        sb.AppendLine($"      \"Username\": \"{AlignmentMapper.Escape(user.TelegramUsername)}\",");
                        sb.AppendLine($"      \"ChatId\": {user.ChatId},");
                        sb.AppendLine($"      \"CreatedAt\": \"{user.CreatedAt:yyyy-MM-dd HH:mm:ss}\",");
                        sb.AppendLine($"      \"UpdatedAt\": \"{user.UpdatedAt:yyyy-MM-dd HH:mm:ss}\",");
                        sb.AppendLine($"      \"LastMessage\": \"{AlignmentMapper.Escape(user.LastMessage)}\"");
                        sb.Append("    }");

                        if (i < sessionsLastDay.Count - 1)
                            sb.AppendLine(",");
                        else
                            sb.AppendLine();
                    }

                    sb.AppendLine("  ]");
                    sb.AppendLine("}");

                    await File.WriteAllTextAsync(path, sb.ToString());

                    await using var stream = File.OpenRead(path);

                    await _bot.SendDocument(
                        chatId: session.ChatId,
                        document: InputFile.FromStream(stream, "Пользователи-за-день.txt"),
                        caption: "Вот ваш файл"
                    );

                    File.Delete(path);
                    break;
                }

            case "Newsletter":
                await SendMessageAsync("Введите сообщение для рассылки (или /cancel для отмены).", session.ChatId);
                session.SessionState.Status = UserSessionStatus.AdminWaitingNewstellerMessage;
                break;

            case "MainDb":
                {
                    var users = _sessionManager.UsersCache.Values.ToList();

                    var sb = new StringBuilder();

                    sb.AppendLine("{");
                    sb.AppendLine($"  \"Count\": {users.Count},");
                    sb.AppendLine("  \"Users\": [");

                    for (int i = 0; i < users.Count; i++)
                    {
                        var user = users[i];

                        sb.AppendLine("    {");
                        sb.AppendLine($"      \"Username\": \"{AlignmentMapper.Escape(user.TelegramUsername)}\",");
                        sb.AppendLine($"      \"ChatId\": {user.ChatId},");
                        sb.AppendLine($"      \"CreatedAt\": \"{user.CreatedAt:yyyy-MM-dd HH:mm:ss}\",");
                        sb.AppendLine($"      \"UpdatedAt\": \"{user.UpdatedAt:yyyy-MM-dd HH:mm:ss}\",");
                        sb.AppendLine($"      \"LastMessage\": \"{AlignmentMapper.Escape(user.LastMessage)}\"");
                        sb.Append("    }");

                        if (i < users.Count - 1)
                            sb.AppendLine(",");
                        else
                            sb.AppendLine();
                    }

                    sb.AppendLine("  ]");
                    sb.AppendLine("}");

                    var fileName = $"{Guid.NewGuid()}.txt";
                    var path = Path.Combine(PathService.ADMIN_PATH, fileName);

                    await File.WriteAllTextAsync(path, sb.ToString());

                    await using var stream = File.OpenRead(path);

                    await _bot.SendDocument(
                        chatId: session.ChatId,
                        document: InputFile.FromStream(stream, "Пользователи.txt"),
                        caption: "Вот ваш файл"
                    );

                    File.Delete(path);
                    break;
                }

            case "Blacklist":
                await SendMessageAsync(await _sessionManager.GetUsersBasedOnStatus(UserSessionStatus.UserSendingUsernameBlacklist), session.ChatId);
                break;

            case "Events":
                {
                    var subscribedUsers = await _eventsDb.GetAllUsersAsync();
                    var sb = new StringBuilder();

                    sb.AppendLine($"Найдено {subscribedUsers.Where(x => !string.IsNullOrEmpty(x.EventsIds)).ToList().Count} подписанных пользователей.");
                    sb.AppendLine();
                    sb.AppendLine(new string('-', 50));
                    sb.AppendLine();

                    foreach (var user in subscribedUsers)
                    {
                        var username = _sessionManager.GetUsernameByChatId(user.UserId) ?? "Unknown";

                        if (string.IsNullOrEmpty(user.EventsIds))
                            continue;

                        sb.AppendLine($"Пользователь: {username} ({user.UserId})");
                        sb.AppendLine();
                        sb.AppendLine("Подписки:");

                        var events = EventService
                            .GetSubscribedEventsUserFormattedMessage(user.EventsIds)
                            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

                        foreach (var ev in events)
                            sb.Append($" - {ev}");

                        sb.AppendLine();
                        sb.AppendLine(new string('-', 50));
                        sb.AppendLine();
                    }

                    var fileName = $"{Guid.NewGuid()}.txt";
                    var path = Path.Combine(PathService.ADMIN_PATH, fileName);

                    File.WriteAllText(path, sb.ToString());

                    await using var stream = File.OpenRead(path);

                    await _bot.SendDocument(
                        chatId: session.ChatId,
                        document: InputFile.FromStream(stream, "Пользователи-события.txt"),
                        caption: "Вот ваш файл"
                    );

                    File.Delete(path);
                    break;
                }

            case "CreatingDocument":
                await SendMessageAsync(await _sessionManager.GetUsersBasedOnStatus(UserSessionStatus.FillingQuestions), session.ChatId);
                break;

            case "Firm":
                await SendMessageAsync(await _sessionManager.GetUsersBasedOnStatus(UserSessionStatus.SendingFirm), session.ChatId);
                break;

            default:
                break;
        }

        return false;
    }

    private async Task DeleteMessagesBeforeMainMenu(UserSession session)
    {
        try
        {
            if (session.ServersState.ServerType == "ВСЕ" && session.ServersState.ServerMessageId != 0)
                await _bot.DeleteMessage(session.ChatId, session.ServersState.ServerMessageId, _cts.Token);

            if (session.ServersState.NotSupportedMessageId != 0)
                await _bot.DeleteMessage(session.ChatId, session.ServersState.NotSupportedMessageId, _cts.Token);

            if (session.ServersState.AlreadyUpdatingMessageId != 0)
                await _bot.DeleteMessage(session.ChatId, session.ServersState.AlreadyUpdatingMessageId, _cts.Token);

            if (session.ServersState.AutoUpdateStoppedMessageId != 0)
                await _bot.DeleteMessage(session.ChatId, session.ServersState.AutoUpdateStoppedMessageId, _cts.Token);

            if (session.EventState.LastEventMessageId != 0)
                await _bot.DeleteMessage(session.ChatId, session.EventState.LastEventMessageId, _cts.Token);

            if (session.EventState.LastUnscribeFromEventMessageId != 0)
                await _bot.DeleteMessage(session.ChatId, session.EventState.LastUnscribeFromEventMessageId, _cts.Token);

            if (session.EventState.LastSubscribeToEventMessageId != 0)
                await _bot.DeleteMessage(session.ChatId, session.EventState.LastSubscribeToEventMessageId, _cts.Token);

            if (session.EventState.LastShowSubscribedMessageId != 0)
                await _bot.DeleteMessage(session.ChatId, session.EventState.LastShowSubscribedMessageId, _cts.Token);

            if (session.BlockedPlayersState.LastBlockedPlayersMessageId != 0)
                await _bot.DeleteMessage(session.ChatId, session.BlockedPlayersState.LastBlockedPlayersMessageId);

            if (session.FirmsState.LastFirmsMessageId != 0)
                await _bot.DeleteMessage(session.ChatId, session.FirmsState.LastFirmsMessageId);
        }
        catch { }
    }

    private async Task ElaborateBackToMenuFromEventsAsync(UserSession session, Update update, ITelegramBotClient client)
    {
        await client.AnswerCallbackQuery(update.CallbackQuery.Id);

        try
        {
            if (session.EventState.LastEventMessageId != 0 && session.EventState.LastEventMessageId != null)
                await _bot.DeleteMessage(session.ChatId, session.EventState.LastEventMessageId, _cts.Token);
        }
        catch { }

        try
        {
            if (session.EventState.LastSubscribeToEventMessageId != 0 && session.EventState.LastSubscribeToEventMessageId != null)
                await _bot.DeleteMessage(session.ChatId, session.EventState.LastSubscribeToEventMessageId, _cts.Token);
        }
        catch { }

        try
        {
            if (session.EventState.LastUnscribeFromEventMessageId != 0 && session.EventState.LastUnscribeFromEventMessageId != null)
                await _bot.DeleteMessage(session.ChatId, session.EventState.LastUnscribeFromEventMessageId, _cts.Token);
        }
        catch { }

        try
        {
            if (session.EventState.LastShowSubscribedMessageId != 0 && session.EventState.LastShowSubscribedMessageId != null)
                await _bot.DeleteMessage(session.ChatId, session.EventState.LastShowSubscribedMessageId, _cts.Token);
        }
        catch { }

        session.EventState.LastEventMessageId = (await _bot.SendMessage(
            chatId: session.ChatId,
            text: "Выберите действие.",
            parseMode: ParseMode.Html,
            replyMarkup: _keyboardFactory.EventsMenu)).MessageId;
    }

    private async Task ElaborateCallbackBackToMenuFromFirmsAsync(UserSession session, Update update, ITelegramBotClient client)
    {
        if (session.FirmsState.LastFirmsMessageId != 0)
        {
            try
            {
                await _bot.DeleteMessage(session.ChatId, session.FirmsState.LastFirmsMessageId);
            }
            catch { }
        }

        await HandleUserMessageAsync("/start", update, session, client);
    }

    private async Task ElaborateCallbackBackToMenuFromHintsAsync(UserSession session, Update update, ITelegramBotClient client)
    {
        await client.AnswerCallbackQuery(update.CallbackQuery.Id);

        try
        {
            if (session.HintState.LastHintMessageId != 0 && session.HintState.LastHintMessageId != null)
                await _bot.DeleteMessage(session.ChatId, session.HintState.LastHintMessageId, _cts.Token);
        }
        catch { }

        await HandleUserMessageAsync("/start", update, session, client);
    }

    private async Task ElaborateCallbackBlacklistAsync(UserSession session, Update update, ITelegramBotClient client, string data)
    {
        await client.AnswerCallbackQuery(update.CallbackQuery.Id, string.Empty, showAlert: false);
        session.BlockedPlayersState.Server = data;

        if (session.BlockedPlayersState.LastBlockedPlayersMessageId != 0)
        {
            try
            {
                await _bot.DeleteMessage(session.ChatId, session.BlockedPlayersState.LastBlockedPlayersMessageId);
            }
            catch { }
        }

        if (!BlacklistPlayersService.BlockedPlayersPerServer.ContainsKey(session.BlockedPlayersState.Server))
        {
            session.BlockedPlayersState.LastBlockedPlayersMessageId = (await _bot.SendMessage(
               chatId: session.ChatId,
               text: $"<b>На данный момент этот сервер не доступен. </b>",
               parseMode: ParseMode.Html,
               replyMarkup: _keyboardFactory.BlacklistMenu)).MessageId;

            return;
        }

        if (!BlacklistPlayersService.IsPlayerBlocked(session.BlockedPlayersState.PlayerName, session.BlockedPlayersState.Server))
        {
            session.BlockedPlayersState.LastBlockedPlayersMessageId = (await _bot.SendMessage(
               chatId: session.ChatId,
               text: $"🙁 Игрок <b>{session.BlockedPlayersState.PlayerName}</b> не найден или не заблокирован на сервере <b>{session.BlockedPlayersState.Server}.</b>",
               parseMode: ParseMode.Html,
               replyMarkup: _keyboardFactory.BlacklistMenu)).MessageId;
        }
        else
        {
            session.BlockedPlayersState.Pages = BlacklistPlayersService.GetBlockedPlayerPages(session.BlockedPlayersState.PlayerName, session.BlockedPlayersState.Server);

            session.BlockedPlayersState.CurrentPage = 0;

            await using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "assets", "blacklist.jpg"));
            var inputFile = InputFile.FromStream(stream);

            session.BlockedPlayersState.LastBlockedPlayersMessageId = (await _bot.SendPhoto(
             chatId: session.ChatId,
             caption: session.BlockedPlayersState.Pages[session.BlockedPlayersState.CurrentPage],
             photo: inputFile,
             parseMode: ParseMode.Html,
             replyMarkup: _keyboardFactory.BuildPagesMenu(session.BlockedPlayersState.CurrentPage, session.BlockedPlayersState.Pages.Count))).MessageId;

            session.SessionState.Status = UserSessionStatus.None;
        }
    }

    private async Task ElaboratCallbackStampAsync(UserSession session, Update update, ITelegramBotClient client, string data)
    {
        await client.AnswerCallbackQuery(update.CallbackQuery.Id, string.Empty, showAlert: false);
        session.FirmsState.Stamp = data;

        if (session.FirmsState.LastFirmsMessageId != 0)
        {
            try
            {
                await _bot.DeleteMessage(chatId: session.ChatId, messageId: session.FirmsState.LastFirmsMessageId);
            }
            catch { }
        }

        await using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "assets", "firms.jpg"));
        var inputFile = InputFile.FromStream(stream);

        session.FirmsState.LastFirmsMessageId = (await _bot.SendPhoto(
        chatId: session.ChatId,
        photo: inputFile,
        replyMarkup: _keyboardFactory.BlacklistMenu,
        caption: "Пришлите фото вашей подписи.\n\n❗<i>Подпись будет накладываться на штамп, так что убедитесь что картинка имеет прозрачный фон, отправляется как документ и имеет формат PNG.</i>\n\n❗<i>Готовое изображение будет иметь случайный наклон вправо или влево на 30°.</i>",
        parseMode: ParseMode.Html)).MessageId;

        session.SessionState.Status = UserSessionStatus.SendingFirm;
    }

    private async Task ElaborateCallbackRemoveEventAsync(UserSession session, Update update, ITelegramBotClient client, string data)
    {
        await client.AnswerCallbackQuery(update.CallbackQuery.Id, string.Empty, showAlert: false);

        if (session.EventState.LastEventMessageId != 0 && session.EventState.LastEventMessageId != session.EventState.LastUnscribeFromEventMessageId)
        {
            try
            {
                await _bot.DeleteMessage(session.ChatId, session.EventState.LastEventMessageId, _cts.Token);
            }
            catch { }
        }

        if (!await EventService.IsUserSubscribedToEvent(_eventsDb, session.ChatId, data))
        {
            session.EventState.LastEventMessageId = (await _bot.SendMessage(
                chatId: session.ChatId,
                text: $"⚠️ Вы не подписаны на: \n<b>\"{EventService.Events.Where(ev => ev.Id == int.Parse(data)).FirstOrDefault()?.Name}\"</b>.",
                parseMode: ParseMode.Html
            )).MessageId;

            return;
        }

        await EventService.RemoveUserSessionEvent(session.ChatId, _eventsDb, data);

        var ev = EventService.Events
            .Where(ev => ev.Id == int.Parse(data))
            .FirstOrDefault();

        if (ev != null)
        {
            var caption = $"✅ Вы отписались от: <b>\"{ev.Name}\"</b>";
            session.EventState.LastEventMessageId = (await _bot.SendMessage(
                chatId: session.ChatId,
                text: caption,
                parseMode: ParseMode.Html
            )).MessageId;
        }
    }

    private async Task HandleCallbackAddEventAsync(UserSession session, Update update, ITelegramBotClient client, string data)
    {
        await client.AnswerCallbackQuery(update.CallbackQuery.Id, string.Empty, showAlert: false);

        if (session.EventState.LastEventMessageId != 0 && session.EventState.LastEventMessageId != session.EventState.LastSubscribeToEventMessageId)
        {
            try
            {
                await _bot.DeleteMessage(session.ChatId, session.EventState.LastEventMessageId, _cts.Token);
            }
            catch { }
        }

        if (await EventService.IsUserSubscribedToEvent(_eventsDb, session.ChatId, data))
        {
            session.EventState.LastEventMessageId = (await _bot.SendMessage(
                chatId: session.ChatId,
                text: $"⚠️ Вы уже подписаны на: \n<b>\"{EventService.Events.Where(ev => ev.Id == int.Parse(data)).FirstOrDefault()?.Name}\"</b>.",
                parseMode: ParseMode.Html
            )).MessageId;

            return;
        }

        await EventService.AddUserSessionEvent(session.ChatId, _eventsDb, data);

        var ev = EventService.Events.Where(ev => ev.Id == int.Parse(data)).FirstOrDefault();

        if (ev != null)
        {
            var caption = EventService.BuildSubscribeEventMessage(ev);
            session.EventState.LastEventMessageId = (await _bot.SendMessage(
                chatId: session.ChatId,
                text: caption,
                parseMode: ParseMode.Html
            )).MessageId;
        }
    }

    private async Task HandleCallbackResultFormatAsync(UserSession session, Update update, ITelegramBotClient client, string data)
    {
        await client.AnswerCallbackQuery(update.CallbackQuery.Id, string.Empty, showAlert: false);

        if (session.DocumentState.LastQuestionMessageId != 0 && session.DocumentState.LastQuestionMessageId != null)
            await _bot.DeleteMessage(session.ChatId, session.DocumentState.LastQuestionMessageId, _cts.Token);

        var resultPath = PathService.GetResultPathDocxResults(session.DocumentState.Fraction, session.ChatId, session.DocumentState.DocumentType);

        switch (data)
        {
            case "Image":
                await SendResultInImagesAsync(session);
                await SendMessageAsync("✅ Готово.\n\n<i>Приносим извинения за водяной знак на документе!</i>", session.ChatId, null, _keyboardFactory.FirstQuestionMenu);
                break;

            case "Links":
                await SendResultInLinks(session);
                await SendMessageAsync("✅ Готово.", session.ChatId, null, _keyboardFactory.FirstQuestionMenu);
                break;

            case "Document":
                await SendResultInDocumentAsync(session);
                break;
        }

        await _sessionManager.ClearUserDataAsync(session, resultPath);
    }

    private async Task HandleCallbackBackToMainMenuAsync(UserSession session, Update update, ITelegramBotClient client, string data)
    {
        try
        {
            await client.AnswerCallbackQuery(update.CallbackQuery.Id, string.Empty, showAlert: false);
            await _bot.DeleteMessage(session.ChatId, messageId: update.CallbackQuery.Message.MessageId);

            if (data == "Да")
            {
                if (session.DocumentState.DocumentMainTitleMessageId != 0 && session.DocumentState.DocumentMainTitleMessageId != null &&
                    session.DocumentState.LastQuestionMessageId != 0 && session.DocumentState.LastQuestionMessageId != null)
                {
                    {
                        await _bot.DeleteMessage(session.ChatId, messageId: session.DocumentState.DocumentMainTitleMessageId);
                        await _bot.DeleteMessage(session.ChatId, messageId: session.DocumentState.LastQuestionMessageId);
                    }

                    var resultPath = PathService.GetResultPathDocxResults(session.DocumentState.Fraction, session.ChatId, session.DocumentState.DocumentType);
                    await _sessionManager.ClearUserDataAsync(session, resultPath);

                    var pathStart = Path.Combine(AppContext.BaseDirectory, "assets", "start-image.jpg");
                    await using var stream = File.OpenRead(pathStart);
                    var inputFile = InputFile.FromStream(stream);

                    session.LastCommandMessageId = (await _bot.SendPhoto(
                        chatId: session.ChatId,
                        caption: Messages.StartMessage,
                        replyMarkup: _keyboardFactory.StartMenuWithoutMainButton,
                        parseMode: ParseMode.Html,
                        photo: inputFile)).MessageId;
                }
            }
            else if (data == "Нет")
            {
                session.SessionState.Status = UserSessionStatus.FillingQuestions;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    private async Task HandleCallbackServerHintsAsync(UserSession session, Update update, ITelegramBotClient client, string data)
    {
        await client.AnswerCallbackQuery(update.CallbackQuery.Id, string.Empty);

        try
        {
            if (session.HintState.LastHintMessageId != 0 && session.HintState.LastHintMessageId != null)
                await _bot.DeleteMessage(session.ChatId, session.HintState.LastHintMessageId, _cts.Token);
        }
        catch { }

        session.HintState.HintsFromServer = data;
        _sessionManager.UpdateSession(session);

        var keyboard = _keyboardFactory.BuildDefaultHints(data);

        if (keyboard is null || keyboard.InlineKeyboard.Count() == 0)
        {
            session.HintState.LastHintMessageId = (await _bot.SendMessage(
                chatId: session.ChatId,
                text: "Подсказки для данного сервера на данный момент отсутствуют.",
                parseMode: ParseMode.Html
            )).MessageId;

            return;
        }

        session.HintState.LastHintMessageId = (await _bot.SendMessage(
            chatId: session.ChatId,
            text: "Выберите подсказку.",
            parseMode: ParseMode.Html,
            replyMarkup: keyboard
        )).MessageId;
    }

    private async Task HandleCallbackStopServersAutoupdateAsync(UserSession session, Update update, ITelegramBotClient client, string data)
    {
        if (data == "Stop")
        {
            await client.AnswerCallbackQuery(update.CallbackQuery.Id);

            try
            {
                if (session.ServersState.ServerMessageId != 0)
                {
                    await _bot.UnpinChatMessage(chatId: session.ChatId, messageId: session.ServersState.ServerMessageId, cancellationToken: _cts.Token);
                    await _bot.DeleteMessage(chatId: session.ChatId, messageId: session.ServersState.ServerMessageId, cancellationToken: _cts.Token);
                }
            }
            catch { }

            try
            {
                if (session.ServersState.AlreadyUpdatingMessageId != 0)
                    await _bot.DeleteMessage(chatId: session.ChatId, messageId: session.ServersState.AlreadyUpdatingMessageId, cancellationToken: _cts.Token);
            }
            catch { }

            try
            {
                if (session.ServersState.NotSupportedMessageId != 0)
                    await _bot.DeleteMessage(chatId: session.ChatId, messageId: session.ServersState.NotSupportedMessageId, cancellationToken: _cts.Token);
            }
            catch { }

            try
            {
                if (session.ServersState.AutoUpdateStoppedMessageId != 0)
                    await _bot.DeleteMessage(chatId: session.ChatId, messageId: session.ServersState.AutoUpdateStoppedMessageId, cancellationToken: _cts.Token);
            }
            catch { }

            session.ServersState.StopServersAutoUpdate();
            _sessionManager.UpdateSession(session);
            _serversUpdater.RemoveUserCache(session.ChatId);

            session.ServersState.AutoUpdateStoppedMessageId = (await _bot.SendMessage(
          chatId: session.ChatId,
          text: "✅ <b>Автообновление остановлено.</b>",
          parseMode: ParseMode.Html,
          replyMarkup: _keyboardFactory.FirstQuestionMenu)).MessageId;
        }
    }

    private async Task HandleCallbackHintsColorBackgroundAsync(UserSession session, string data, ITelegramBotClient client, Update update)
    {
        if (session.HintState.LastHintMessageId != 0 && session.HintState.LastHintMessageId != null)
        {
            try
            {
                await _bot.DeleteMessage(session.ChatId, session.HintState.LastHintMessageId, _cts.Token);
            }
            catch { }
        }

        var backgroundColor = data;
        var path = Path.Combine(PathService.HINTS_PATH, session.HintState.HintsFromServer, $"{session.HintState.HintName}_{backgroundColor}.png");

        await client.AnswerCallbackQuery(update.CallbackQuery.Id);

        await using var stream = File.OpenRead(path);
        await _bot.SendDocument(
             chatId: session.ChatId,
             document: InputFile.FromStream(stream, Path.GetFileName(path)),
             caption: $"Подсказка для <b>{session.HintState.HintName} - {backgroundColor} фон</b>\n\n<b>🤖 Сделано с помощью ПротоКот:</b> \n@TheProtoKot",
             parseMode: ParseMode.Html);
    }

    private async Task HandleCallbackHintsAsync(UserSession session, string data, ITelegramBotClient client, Update update)
    {
        if (session.HintState.LastHintMessageId != 0 && session.HintState.LastHintMessageId != null)
        {
            try
            {
                await _bot.DeleteMessage(session.ChatId, session.HintState.LastHintMessageId, _cts.Token);
            }
            catch { }
        }

        session.HintState.HintName = data;
        _sessionManager.UpdateSession(session);

        await client.AnswerCallbackQuery(update.CallbackQuery.Id, string.Empty, showAlert: false);
        session.HintState.LastHintMessageId = (await _bot.SendMessage(
            chatId: session.ChatId,
            text: $"Выберите цвет фона для подсказки.",
            parseMode: ParseMode.Html,
            replyMarkup: _keyboardFactory.BackgroundColorHint
        )).MessageId;
    }

    private async Task HandleCallbackDocumentTypeAsync(UserSession session, Update update, ITelegramBotClient client, string data)
    {
        var callbackId = update.CallbackQuery.Id;
        await client.AnswerCallbackQuery(callbackId, string.Empty, showAlert: false);

        session.DocumentState.DocumentType = data;
        session.SessionState.Status = UserSessionStatus.FillingQuestions;

        var text = $"Пример заполнения для <b>\"{data}\".</b>\n\nОтветьте на несколько вопросов для заполнения документа.";
        var caption = DateService.AddDateToQuestion(QuestionsConstants.FIRST_QUESTION);
        var inlineKeyboard = _keyboardFactory.BuildDateHintsMenu(QuestionsConstants.FIRST_QUESTION);

        var instructionPhotoPath = PathService.GetInstructionPath(session.DocumentState.Fraction, session.DocumentState.DocumentType);
        await using var stream = File.OpenRead(instructionPhotoPath);
        var inputFile = InputFile.FromStream(stream);

        if (session.DocumentState.LastQuestionMessageId != 0 && session.DocumentState.LastQuestionMessageId != null)
            await _bot.DeleteMessage(chatId: session.ChatId, messageId: session.DocumentState.LastQuestionMessageId);

        var sentMessageMainTitle = await _bot.SendPhoto(
            chatId: session.ChatId,
            photo: inputFile,
            replyMarkup: _keyboardFactory.FirstQuestionMenu,
            cancellationToken: _cts.Token,
            parseMode: ParseMode.Html,
            caption: text);

        var sentMessageText = await _bot.SendMessage(
                  cancellationToken: _cts.Token,
                  chatId: session.ChatId,
                  text: caption,
                  parseMode: ParseMode.Html,
                  replyMarkup: inlineKeyboard
              );

        session.DocumentState.DocumentMainTitleMessageId = sentMessageMainTitle.MessageId;
        session.DocumentState.LastQuestionMessageId = sentMessageText.MessageId;
        _sessionManager.UpdateSession(session);
    }

    private async Task HandleCallbackFractionsAsync(UserSession session, Update update, ITelegramBotClient client, string data)
    {
        await client.AnswerCallbackQuery(update.CallbackQuery.Id);

        try
        {
            var documentsFractionKeyboard = _keyboardFactory.BuildDocumentTypes(data);
            var sentMessage = await _bot.EditMessageCaption(chatId: session.ChatId, messageId: session.DocumentState.LastQuestionMessageId, caption: "Выберите тип документа.", replyMarkup: documentsFractionKeyboard, parseMode: ParseMode.Html);

            session.DocumentState.LastQuestionMessageId = sentMessage.MessageId;
            session.DocumentState.Fraction = data;
            _sessionManager.UpdateSession(session);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message + "\n\t" + e.Source);
        }
    }

    private async Task HandleCallbackBotCommandsAsync(UserSession session, Update update, ITelegramBotClient client, string data)
    {
        switch (data)
        {
            case "Documents":
                await client.AnswerCallbackQuery(update.CallbackQuery.Id);
                await HandleUserMessageAsync("/documents", update, session, client);
                break;

            default:
                await client.AnswerCallbackQuery(update.CallbackQuery.Id, "❌ Неизвестная команда", showAlert: true);
                break;
        }
    }

    private async Task HandleCallbackServersAsync(UserSession session, Update update, ITelegramBotClient client, string data)
    {
        await client.AnswerCallbackQuery(update.CallbackQuery.Id);

        if (!string.IsNullOrEmpty(session.ServersState.ServerType) && session.ServersState.ServerType != "ВСЕ")
        {
            if (session.ServersState.AlreadyUpdatingMessageId != 0)
            {
                try
                {
                    await _bot.DeleteMessage(chatId: session.ChatId, messageId: session.ServersState.AlreadyUpdatingMessageId);
                }
                catch { }
            }

            session.ServersState.AlreadyUpdatingMessageId = (await _bot.SendMessage(
                chatId: session.ChatId,
                text: "<b>Информация о серверах уже отображается.</b>\nПожалуйста, дождитесь её обновления или остановите автообновление.",
                parseMode: ParseMode.Html,
                replyMarkup: _keyboardFactory.ServersUpdateMenu,
                cancellationToken: _cts.Token
                )).MessageId;

            return;
        }

        if (session.ServersState.ServerType == "ВСЕ" && session.ServersState.ServerMessageId != 0)
        {
            try
            {
                await _bot.DeleteMessage(chatId: session.ChatId, messageId: session.ServersState.ServerMessageId, cancellationToken: _cts.Token);
            }
            catch { }
        }

        session.ServersState.ServerType = data;
        var info = await ServerService.GetServerInfoMessage(data, _serversServiceUrl);

        if (string.IsNullOrEmpty(info) || info == "Сервер не найден.")
        {
            session.ServersState.NotSupportedMessageId = (await _bot.SendMessage(
               chatId: session.ChatId,
               text: "Не удалось получить информацию о серверах. Пожалуйста, попробуйте позже.",
               parseMode: ParseMode.Html,
               cancellationToken: _cts.Token

               )).MessageId;

            session.ServersState.StopServersAutoUpdate();
            return;
        }

        session.ServersState.ServersMessageAutoUpdate = info;
        session.ServersState.ServerMessageId = (await _bot.SendMessage(
                           chatId: session.ChatId,
                           text: session.ServersState.ServersMessageAutoUpdate,
                           parseMode: ParseMode.Html,
                           replyMarkup: data == "ВСЕ" ? null : _keyboardFactory.ServersUpdateMenu,
                           cancellationToken: _cts.Token
                           )).MessageId;

        if (data != "ВСЕ")
        {
            await _bot.PinChatMessage(
                chatId: session.ChatId,
                messageId: session.ServersState.ServerMessageId,
                disableNotification: true,
                cancellationToken: _cts.Token);

            session.ServersState.ServersUpdateEnabled = true;
            _sessionManager.UpdateSession(session);
        }
    }
}
