using AmazingBot;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

internal sealed partial class TelegramBot
{
    private async Task HandleUserMessageAsync(string message, Update update, UserSession session, ITelegramBotClient client)
    {
        if (!await CheckSubscribe(session))
            return;

        try
        {
            switch (message)
            {
                case "🏠 Главное меню":
                case "/start":
                    await DeleteMessagesBeforeMainMenu(session);
                    await ElaborateStartMenuAsync(update, session);
                    break;

                case "/admin":
                     await ElaborateAdminPanelAsync(update, session);
                    break;

                case "🏠 Домой":
                    await ElaborateHomeAsync(update, session);
                    break;

                case "⏪ Вернуться к предыдущему вопросу":
                    await ElaborateBackToPreviousQuestionAsync(update, session);
                    break;

                case "📄 Документы":
                    await ElaborateCreateDocumentAsync(update, session);
                    break;

                case "🌐 Мониторинг серверов":
                    await ElaborateServersOnlineAsync(update, session);
                    break;

                case "💡 Подсказки":
                    await ElaborateHintsAsync(update, session);
                    break;

                case "/coder":
                    await ElaborateCoderInformationAsync(update, session);
                    break;

                case "/report":
                    await ElaborateHelpAsync(update, session);
                    break;

                case "📅 События и мероприятия":
                    await ElaborateEventsAsync(update, session);
                    break;

                case "➕ Подписаться":
                    await ElaborateSubscribeToEventAsync(update, session);
                    break;

                case "📋 Показать подписанные":
                    await ElaborateShowSubscribedAsync(update, session);
                    break;

                case "❌ Отписаться":
                    await ElaborateUnscribeAsync(update, session);
                    break;

                case "🚫 Чёрный список":
                    await ElaborateBlockedPlayersAsync(update, session);
                    break;

                case "✍️ Подписи":
                    await ElaborateFirmsAsync(update, session);
                    break;

                case "🔔 Чекер жалоб (NEW)":
                    await ElaborateReportCheckerAsync(update, session);
                    break;

                default:
                    if (session.SessionState.Status != UserSessionStatus.None || message is null)
                        return;

                    if (message.StartsWith('/'))
                        await SendMessageAsync("🚫 Неизвестная команда.", session.ChatId);

                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private async Task ElaborateFirmsAsync(Update update, UserSession session)
    {
        await _bot.DeleteMessage(session.ChatId, update.Message.MessageId);

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
    }

    private async Task ElaborateReportCheckerAsync(Update update, UserSession session)
    {
        await _bot.DeleteMessage(session.ChatId, update.Message.MessageId);

        if (session.LastCommandMessageId != 0)
        {
            try
            {
                await _bot.DeleteMessage(chatId: session.ChatId, messageId: session.LastCommandMessageId);
            }
            catch { }
        }

        await using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "assets", "report-checker.jpg"));
        var inputFile = InputFile.FromStream(stream);

        session.ReportCheckerState.ReportCheckerMessageId = session.LastCommandMessageId = (await _bot.SendPhoto(
        chatId: session.ChatId,
        photo: inputFile,
        replyMarkup: _keyboardFactory.ReportCheckerMenu,
        parseMode: ParseMode.Html,
        caption: Messages.ReportCheckerMessage
        )).MessageId;
    }

    private async Task ElaborateHomeAsync(Update update, UserSession session)
    {
        if (session.BlockedPlayersState.LastBlockedPlayersMessageId != 0)
        {
            try
            {
                await _bot.DeleteMessage(session.ChatId, session.BlockedPlayersState.LastBlockedPlayersMessageId);
            }
            catch { }
        }

        if (session.FirmsState.LastFirmsMessageId != 0)
        {
            try
            {
                await _bot.DeleteMessage(session.ChatId, session.FirmsState.LastFirmsMessageId);
            }
            catch { }
        }

        await ElaborateStartMenuAsync(update, session);
    }

    private async Task<bool> ElaborateAdminPanelAsync(Update update, UserSession session)
    {
        await _bot.DeleteMessage(chatId: session.ChatId, messageId: update.Message.MessageId);

        if (!TelegramConstants.Admins.Contains(session.ChatId))
        {
            session.LastCommandMessageId = (await _bot.SendMessage(
           chatId: session.ChatId,
           text: "🚫 Неизвестная команда.",
           replyMarkup: _keyboardFactory.FirstQuestionMenu,
           parseMode: ParseMode.Html)).MessageId;

            return false;
        }

        await SendMessageAsync("<b>Добро пожаловать в админ панель.</b>", session.ChatId, _keyboardFactory.AdminPanel);
        return true;
    }

    private async Task ElaborateCoderInformationAsync(Update update, UserSession session)
    {
        await _bot.DeleteMessage(session.ChatId, update.Message.MessageId);

        if (session.LastCommandMessageId != 0)
        {
            try
            {
                await _bot.DeleteMessage(chatId: session.ChatId, messageId: session.LastCommandMessageId);
            }
            catch { }
        }

        session.LastCommandMessageId = (await _bot.SendMessage(
            chatId: session.ChatId,
            replyMarkup: _keyboardFactory.StartMenu,
            text: Messages.AuthorMessage,
            parseMode: ParseMode.Html)).MessageId;
    }

    private async Task ElaborateBlockedPlayersAsync(Update update, UserSession session)
    {
        await _bot.DeleteMessage(session.ChatId, update.Message.MessageId);

        if (session.BlockedPlayersState.LastBlockedPlayersMessageId != 0)
        {
            try
            {
                await _bot.DeleteMessage(session.ChatId, session.BlockedPlayersState.LastBlockedPlayersMessageId);
            }
            catch { }
        }

        if (session.LastCommandMessageId != 0)
        {
            try
            {
                await _bot.DeleteMessage(chatId: session.ChatId, messageId: session.LastCommandMessageId);
            }
            catch { }
        }

        await using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "assets", "blacklist.jpg"));
        var inputFile = InputFile.FromStream(stream);

        if (!_blacklistTablesDownloader._isDataLoaded)
        {
            session.LastCommandMessageId = session.BlockedPlayersState.LastBlockedPlayersMessageId = (await _bot.SendPhoto(
             chatId: session.ChatId,
             photo: inputFile,
             replyMarkup: _keyboardFactory.StartMenu,
             caption: "<b>⏳ Идет загрузка данных...</b>\nПожалуйста подождите 10 секунд.",
             parseMode: ParseMode.Html)).MessageId;

            return;
        }

        session.LastCommandMessageId = (await _bot.SendPhoto(
             chatId: session.ChatId,
             photo: inputFile,
             replyMarkup: _keyboardFactory.BlacklistMenu,
             caption: Messages.BlacklistMessage,
             parseMode: ParseMode.Html)).MessageId;

        session.SessionState.Status = UserSessionStatus.UserSendingUsernameBlacklist;
    }

    private async Task ElaborateUnscribeAsync(Update update, UserSession session)
    {
        await _bot.DeleteMessage(session.ChatId, update.Message.MessageId);

        if (session.EventState.LastEventMessageId != 0 && session.EventState.LastEventMessageId != null)
        {
            try
            {
                await _bot.DeleteMessage(session.ChatId, session.EventState.LastEventMessageId);
            }
            catch { }
        }

        if (!await EventService.IsUserSubscribedToAny(session.ChatId, _eventsDb))
        {
            session.EventState.LastEventMessageId = (await _bot.SendMessage(
                chatId: session.ChatId,
                text: "<b>⚠️ Вы ещё не подписаны на активные события!</b>",
                replyMarkup: _keyboardFactory.EventsMenu,
                parseMode: ParseMode.Html)).MessageId;

            return;
        }

        var text = "Выберите событие / мероприятие от которого хотите отписаться.";
        session.EventState.LastUnscribeFromEventMessageId = session.EventState.LastEventMessageId = (await _bot.SendMessage(
             chatId: session.ChatId,
             text: text,
             replyMarkup: await _keyboardFactory.BuildRemoveEvents(_eventsDb, session.ChatId),
             parseMode: ParseMode.Html)).MessageId;
    }

    private async Task ElaborateShowSubscribedAsync(Update update, UserSession session)
    {
        await _bot.DeleteMessage(session.ChatId, update.Message.MessageId);

        if (session.EventState.LastEventMessageId != 0 && session.EventState.LastEventMessageId != null)
        {
            try
            {
                await _bot.DeleteMessage(session.ChatId, session.EventState.LastEventMessageId);
            }
            catch { }
        }

        if (!await EventService.IsUserSubscribedToAny(session.ChatId, _eventsDb))
        {
            session.EventState.LastEventMessageId = (await _bot.SendMessage(
                chatId: session.ChatId,
                text: "<b>⚠️ Вы ещё не подписаны на активные события / мероприятия!</b>",
                replyMarkup: _keyboardFactory.EventsMenu,
                parseMode: ParseMode.Html)).MessageId;
            return;
        }

        var text = await EventService.BuildUserEventsMessage(_eventsDb, session.ChatId);
        session.EventState.LastShowSubscribedMessageId = session.EventState.LastEventMessageId = (await _bot.SendMessage(
            chatId: session.ChatId,
            text: text,
            replyMarkup: _keyboardFactory.ReturnToEventMenu,
            parseMode: ParseMode.Html)).MessageId;
    }

    private async Task ElaborateSubscribeToEventAsync(Update update, UserSession session)
    {
        await _bot.DeleteMessage(session.ChatId, update.Message.MessageId);

        if (session.EventState.LastEventMessageId != 0 && session.EventState.LastEventMessageId != null)
        {
            try
            {
                await _bot.DeleteMessage(session.ChatId, session.EventState.LastEventMessageId);
            }
            catch { }
        }

        var text = "Выберите событие / мероприятие на которое хотите подписаться.";
        session.EventState.LastSubscribeToEventMessageId = session.EventState.LastEventMessageId = (await _bot.SendMessage(
            chatId: session.ChatId,
            text: text,
            replyMarkup: _keyboardFactory.BuildEventButtons(),
            parseMode: ParseMode.Html)).MessageId;
    }

    private async Task ElaborateEventsAsync(Update update, UserSession session)
    {
        await _bot.DeleteMessage(session.ChatId, update.Message.MessageId);

        if (session.LastCommandMessageId != 0)
        {
            try
            {
                await _bot.DeleteMessage(chatId: session.ChatId, messageId: session.LastCommandMessageId);
            }
            catch { }
        }

        await _sessionManager.ClearUserDataAsync(session, "");
        var pathHints = Path.Combine(PathService.BASE_PATH, "assets", "events.jpg");
        await using var stream = File.OpenRead(pathHints);
        var inputFile = InputFile.FromStream(stream);

        var sentMessage = await _bot.SendPhoto(
            chatId: session.ChatId,
            photo: inputFile,
            replyMarkup: _keyboardFactory.EventsMenu,
            caption: Messages.EventsMessage,
            parseMode: ParseMode.Html);

        session.LastCommandMessageId = sentMessage.MessageId;
        return;
    }

    private async Task ElaborateHelpAsync(Update update, UserSession session)
    {
        await _bot.DeleteMessage(chatId: session.ChatId, messageId: update.Message.MessageId);

        if (session.LastCommandMessageId != 0)
        {
            try
            {
                await _bot.DeleteMessage(chatId: session.ChatId, messageId: session.LastCommandMessageId);
            }
            catch { }
        }

        var keyboard = new InlineKeyboardButton[][]
        {
                    new InlineKeyboardButton[]
                    {
                        InlineKeyboardButton.WithUrl("Перейти в чат", "https://t.me/TheProtoKot?direct")
                    },
                      new InlineKeyboardButton[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔙 Вернуться","MainMenu@Report")
                    }
        };

        await _sessionManager.ClearUserDataAsync(session, "");
        var pathHints = Path.Combine(PathService.BASE_PATH, "assets", "report.jpg");
        await using var stream = File.OpenRead(pathHints);
        var inputFile = InputFile.FromStream(stream);
        var caption = "👇 Чтобы предложить идею или подать жалобу, перейдите в этот чат.\nБудем искренне благодарны каждому, за обнаружение багов и недочётов.";

        var sentMessage = await _bot.SendPhoto(
            chatId: session.ChatId,
            photo: inputFile,
            replyMarkup: keyboard,
            caption: caption,
            parseMode: ParseMode.Html);

        session.LastCommandMessageId = sentMessage.MessageId;
    }

    private async Task ElaborateHintsAsync(Update update, UserSession session)
    {
        await _bot.DeleteMessage(chatId: session.ChatId, messageId: update.Message.MessageId);

        if (session.LastCommandMessageId != 0 && session.LastCommandMessageId != null)
        {
            try
            {
                await _bot.DeleteMessage(chatId: session.ChatId, messageId: session.LastCommandMessageId);
            }
            catch { }
        }

        await _sessionManager.ClearUserDataAsync(session, "");
        var pathHints = Path.Combine(PathService.BASE_PATH, "assets", "hints.jpg");
        await using var stream = File.OpenRead(pathHints);
        var inputFile = InputFile.FromStream(stream);
        var caption = "Выберите сервер.";

        var sentMessage = await _bot.SendPhoto(
            chatId: session.ChatId,
            photo: inputFile,
            replyMarkup: _keyboardFactory.ServersHintsMenu,
            caption: caption,
            parseMode: ParseMode.Html);

        session.LastCommandMessageId = sentMessage.MessageId;
        return;
    }

    private async Task ElaborateServersOnlineAsync(Update update, UserSession session)
    {
        await _bot.DeleteMessage(chatId: session.ChatId, messageId: update.Message.MessageId);

        if (session.LastCommandMessageId != 0)
        {
            try
            {
                await _bot.DeleteMessage(chatId: session.ChatId, messageId: session.LastCommandMessageId);
            }
            catch { }
        }

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

            var keyboard = new InlineKeyboardMarkup(new[]{
                new[]{ InlineKeyboardButton.WithCallbackData("⛔ Остановить", "ServersUpdate@Stop")},
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Вернуться","MainMenu@ServersOnline") }});

            session.ServersState.AlreadyUpdatingMessageId = (await _bot.SendMessage(
                 chatId: session.ChatId,
                 text: "<b>Информация о серверах уже отображается.</b>\nПожалуйста, дождитесь её обновления или остановите автообновление.",
                parseMode: ParseMode.Html,
                 replyMarkup: keyboard,
                 cancellationToken: _cts.Token
                 )).MessageId;

            return;
        }

        var pathServersInfo = Path.Combine(PathService.BASE_PATH, "assets", "servers-info.jpg");
        FileStream stream = File.OpenRead(pathServersInfo);
        var inputFile = InputFile.FromStream(stream);

        var sentMessage = await _bot.SendPhoto(
            chatId: session.ChatId,
            photo: inputFile,
            replyMarkup: _keyboardFactory.Servers,
            caption: Messages.ServerMonitorMessage,
            parseMode: ParseMode.Html);

        session.LastCommandMessageId = sentMessage.MessageId;
    }

    private async Task ElaborateCreateDocumentAsync(Update update, UserSession session)
    {
        await _bot.DeleteMessage(chatId: session.ChatId, messageId: update.Message.MessageId);

        if (session.LastCommandMessageId != 0 && session.LastCommandMessageId != null)
        {
            try
            {
                await _bot.DeleteMessage(chatId: session.ChatId, messageId: session.LastCommandMessageId);
            }
            catch { }
        }

        await _sessionManager.ClearUserDataAsync(session, "");
        var pathDocuments = Path.Combine(AppContext.BaseDirectory, "assets", "create-document.jpg");
        FileStream stream = File.OpenRead(pathDocuments);
        var inputFile = InputFile.FromStream(stream);

        var sentMessage = await _bot.SendPhoto(
      chatId: session.ChatId,
      photo: inputFile,
      replyMarkup: _keyboardFactory.DocumentMenu,
      caption: Messages.DocumentCreationMessage,
      parseMode: ParseMode.Html);

        session.LastCommandMessageId = sentMessage.MessageId;
    }

    private async Task ElaborateBackToPreviousQuestionAsync(Update update, UserSession session)
    {
        await _bot.DeleteMessage(chatId: session.ChatId, messageId: update.Message.MessageId);
        await BackToPreviousQuestionAsync(session, update);
    }

    private async Task ElaborateStartMenuAsync(Update update, UserSession session)
    {
        try
        {
            if (update.Message is not null)
                await _bot.DeleteMessage(chatId: session.ChatId, messageId: update.Message.MessageId);
        }
        catch { }

        if (session.LastCommandMessageId != 0)
        {
            try
            {
                await _bot.DeleteMessage(chatId: session.ChatId, messageId: session.LastCommandMessageId);
            }
            catch { }
        }

        if (session.SessionState.Status == UserSessionStatus.FillingQuestions ||
            session.SessionState.Status == UserSessionStatus.BackingToPreviousQuestion)
        {
            if (session.DocumentState.Step >= 1)
            {
                session.SessionState.Status = UserSessionStatus.None;
                session.LastUpdateMessageId = update.Message.MessageId;
                _sessionManager.UpdateSession(session);
                await SendMessageAsync("Вы уверены? Все несохранённые данные будут потеряны.", session.ChatId, _keyboardFactory.ConfrimBackToMainMenu);
                return;
            }
            else
            {
                try
                {
                    if (session.DocumentState.DocumentMainTitleMessageId != 0 &&
                        session.DocumentState.LastQuestionMessageId != 0)
                    {
                        await _bot.DeleteMessage(session.ChatId, messageId: session.DocumentState.DocumentMainTitleMessageId);
                        await _bot.DeleteMessage(session.ChatId, messageId: session.DocumentState.LastQuestionMessageId);
                    }
                }
                catch { }
            }
        }

        await _sessionManager.ClearUserDataAsync(session, "");

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
