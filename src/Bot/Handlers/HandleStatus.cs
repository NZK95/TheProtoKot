using AmazingBot;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

internal sealed partial class TelegramBot
{
    private async Task HandleStatusAsync(UserSession session, Update update)
    {
        try
        {
            switch (session.SessionState.Status)
            {
                case UserSessionStatus.BackingToPreviousQuestion:
                    await HandleBackToQuestionAsync(session, update);
                    break;

                case UserSessionStatus.FillingQuestions:
                    await HandleQuestionsAsync(session, update);
                    break;

                case UserSessionStatus.UserSendingUsernameBlacklist:
                    await ElaborateUsernameSendingAsync(session, update);
                    break;

                case UserSessionStatus.SendingFirm:
                    await HandleUserFirmStatusAsync(session, update);
                    break;

                case UserSessionStatus.AdminWaitingLimitsUsername:
                    await ElaborateFillingLimitsAsync(session, update);
                    break;

                case UserSessionStatus.UserFillingDataReportChecker:
                    await ElaborateDataForAccountCheckedForReportsAsync(session, update);
                    break;

                case UserSessionStatus.AdminWaitingBanUsername:
                    await ElaborateBanUsernameFromAdmin(session, update);
                    break;

                case UserSessionStatus.AdminWaitingKickUsername:
                    await ElaborateKickUsernameFromAdminAsync(session, update);
                    break;

                case UserSessionStatus.AdminWaitingNewstellerMessage:
                    await ElaborateNewstellerFromAdminAsync(session, update);
                    break;

                case UserSessionStatus.None:
                default:
                    break;
            }
        }
        catch
        {
            await SendMessageAsync("❌ <b>Произошла ошибка.</b>\nПожалуйста, попробуйте ещё раз.", session.ChatId);
        }
    }

    private async Task<bool> ElaborateDataForAccountCheckedForReportsAsync(UserSession session, Update update)
    {
        var message = update?.Message?.Text ?? "";
        await _bot.DeleteMessage(session.ChatId, update.Message.MessageId);

        if (message == "/cancel")
        {
            var accounts = await _rcDb.GetAccountsAsync(session.ChatId);
            int limit = await _rcDb.GetAccountLimitAsync(session.ChatId);
            int count = await _rcDb.GetAccountsCountAsync(session.ChatId);

            await _bot.EditMessageCaption(
chatId: session.ChatId,
messageId: session.ReportCheckerState.ReportCheckerMessageId,
replyMarkup: _keyboardFactory.BuildListOfTrackedForReportsPlayersShowPropertiesMenu(accounts, count >= limit ? false : true, true),
parseMode: ParseMode.Html,
caption: "👥 Ваши аккаунты:");

            session.SessionState.Status = UserSessionStatus.None;
            return false;
        }

        switch (session.ReportCheckerState.Step)
        {
            case 0:
                if (!BlacklistPlayersService.IsValidNickname(message))
                {
                    await _bot.EditMessageCaption(
                    messageId: session.ReportCheckerState.ReportCheckerMessageId,
                     chatId: session.ChatId,
                     parseMode: ParseMode.Html,
                     caption: "🚫 <b>Никнейм в неверном формате!</b>\n" +
                         "Никнейм должен быть в формате: <b>Имя_Фамилия</b> на английском.\n" +
                         "Пожалуйста, повторите ввод:");

                    return false;
                }

                await _bot.EditMessageCaption(
messageId: session.ReportCheckerState.ReportCheckerMessageId,
chatId: session.ChatId,
parseMode: ParseMode.Html,
caption: $"✅ Отлично, <b>{message}</b>!\n\n🆔 Теперь введите ID аккаунта:");

                session.ReportCheckerState.Nickname = message;
                session.ReportCheckerState.Step++;
                break;

            case 1:
                if (!long.TryParse(message, out var accountId))
                {
                    await _bot.EditMessageCaption(
messageId: session.ReportCheckerState.ReportCheckerMessageId,
chatId: session.ChatId,
parseMode: ParseMode.Html,
caption: "🚫 <b>ID аккаунта должен быть числом!</b>\nПожалуйста, введите корректный ID:");

                    return false;
                }

                session.ReportCheckerState.Step++;
                session.SessionState.Status = UserSessionStatus.None;
                session.ReportCheckerState.AccountID = accountId;

                await _bot.EditMessageCaption(
                    messageId: session.ReportCheckerState.ReportCheckerMessageId,
chatId: session.ChatId,
parseMode: ParseMode.Html,
caption: "✅ Отлично! Теперь укажите сервер:",
replyMarkup: _keyboardFactory.ReportCheckerServersMenu);

                break;

            default:
                break;
        }

        return true;
    }

    private async Task<bool> ElaborateBanUsernameFromAdmin(UserSession session, Update update)
    {
        var message = update?.Message?.Text ?? "";
        var username = update?.Message?.Text.Replace("@", "").Trim();

        if (message == "/cancel")
        {
            await SendMessageAsync("Операция отменена.", session.ChatId);
            session.SessionState.Status = UserSessionStatus.None;
            return false;
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            await SendMessageAsync("Повторите попытку.", session.ChatId);
            return false;
        }

        var chatId = _sessionManager.GetChatIdByUsername(username);

        if (chatId == null)
        {
            await SendMessageAsync("<b>Пользователь не найден.</b>", session.ChatId);
            session.SessionState.Status = UserSessionStatus.None;
            return false;
        }

        await _bot.BanChatMember(TelegramConstants.BotChannelName, (long)chatId);
        await SendMessageAsync("<b>Пользователь успешно забанен!</b>", session.ChatId);
        session.SessionState.Status = UserSessionStatus.None;
        return true;
    }

    private async Task<bool> ElaborateKickUsernameFromAdminAsync(UserSession session, Update update)
    {
        var message = update?.Message?.Text ?? "";
        var username = message.Replace("@", "").Trim();

        if (message == "/cancel")
        {
            await SendMessageAsync("Операция отменена.", session.ChatId);
            session.SessionState.Status = UserSessionStatus.None;
            return false;
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            await SendMessageAsync("Повторите попытку.", session.ChatId);
            return false;
        }

        var chatId = _sessionManager.GetChatIdByUsername(username);

        if (chatId == null)
        {
            await SendMessageAsync("<b>Пользователь не найден.</b>", session.ChatId);
            session.SessionState.Status = UserSessionStatus.None;
            return false;
        }

        await _bot.BanChatMember(TelegramConstants.BotChannelName, (long)chatId);
        await _bot.UnbanChatMember(TelegramConstants.BotChannelName, (long)chatId);
        await SendMessageAsync("<b>Пользователь успешно изгнан!</b>", session.ChatId);
        session.SessionState.Status = UserSessionStatus.None;
        return true;
    }

    private async Task<bool> ElaborateNewstellerFromAdminAsync(UserSession session, Update update)
    {
        var message = update?.Message?.Text ?? "";

        if (string.IsNullOrEmpty(message))
        {
            await SendMessageAsync("Повторите попытку.", session.ChatId);
            return false;
        }

        if (message == "/cancel")
        {
            await SendMessageAsync("Рассылка отменена.", session.ChatId);
            session.SessionState.Status = UserSessionStatus.None;
            return false;
        }

        foreach (var user in _sessionManager.UsersCache.Values)
            await SendMessageAsync(message, user.ChatId);

        await SendMessageAsync
            ($"<b>Рассылка была отправлена {_sessionManager.UsersCache.Count} пользователям успешно!</b>",
            session.ChatId);
        session.SessionState.Status = UserSessionStatus.None;
        return true;
    }

    private async Task<bool> ElaborateFillingLimitsAsync(UserSession session, Update update)
    {
        var message = update?.Message?.Text ?? "";
        await _bot.DeleteMessage(session.ChatId, update.Message.MessageId);

        if (message == "/cancel")
        {
            session.SessionState.Status = UserSessionStatus.None;
            await SendMessageAsync($"✅ Операция отменена", session.ChatId, null);
            return true;
        }

        var chatId = long.Parse(message.Split('-')[0]);
        var limits = int.Parse(message.Split('-')[1]);

        await _rcDb.SetAccountLimitAsync(chatId, limits);
        await SendMessageAsync($"✅ {_sessionManager.GetUsernameByChatId(chatId)} ({chatId}) получил {limits} лимитов!", session.ChatId);
        await SendMessageAsync($"✅ Вы получили {limits} лимитов от факера!", chatId);
        session.SessionState.Status = UserSessionStatus.None;
        return false;
    }

    private async Task ElaborateUsernameSendingAsync(UserSession session, Update update)
    {
        var text = update?.Message?.Text;

        if (string.IsNullOrEmpty(text) || text == "🚫 Чёрный список")
            return;

        if (session.BlockedPlayersState.LastBlockedPlayersMessageId != 0)
        {
            try
            {
                await _bot.DeleteMessage(session.ChatId, session.BlockedPlayersState.LastBlockedPlayersMessageId);
            }
            catch { }
        }

        await _bot.DeleteMessage(session.ChatId, update.Message.MessageId);

        if (!BlacklistPlayersService.IsValidNickname(text))
        {
            session.BlockedPlayersState.LastBlockedPlayersMessageId = (await _bot.SendMessage(
    chatId: session.ChatId,
    text: "🚫 <b>Никнейм в неверном формате!</b>\nНикнейм должен быть в формате: <b>Имя_Фамилия</b> на английском.\nПожалуйста, повторите ввод:",
    parseMode: ParseMode.Html)).MessageId;

            return;
        }

        session.BlockedPlayersState.PlayerName = text;

        await using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "assets", "blacklist.jpg"));
        var inputFile = InputFile.FromStream(stream);

        if (session.LastCommandMessageId != 0)
        {
            try
            {
                await _bot.DeleteMessage(session.ChatId, session.LastCommandMessageId);
            }
            catch { }
        }

        session.BlockedPlayersState.LastBlockedPlayersMessageId = session.LastCommandMessageId = (await _bot.SendPhoto(
        chatId: session.ChatId,
        photo: inputFile,
        replyMarkup: _keyboardFactory.ServersBlockedMenu,
        caption: "Выберите сервер.",
        parseMode: ParseMode.Html)).MessageId;

        session.SessionState.Status = UserSessionStatus.None;
    }

    private async Task HandleUserFirmStatusAsync(UserSession session, Update update)
    {
        if (update.Message?.Photo != null && update.Message.Photo.Length > 0)
        {
            await _bot.DeleteMessage(session.ChatId, update.Message.MessageId);
            await SendFirmError(session, "<b>Пришлите файл!</b>\n\nОтправьте PNG как файл, а не как фото.");
            return;
        }

        if ((update.Message?.Photo == null || update.Message.Photo.Length == 0) && update.Message?.Document == null)
        {
            await _bot.DeleteMessage(session.ChatId, update.Message.MessageId);

            if (session.FirmsState.LastFirmsMessageId != 0)
            {
                try
                {
                    await _bot.DeleteMessage(session.ChatId, session.FirmsState.LastFirmsMessageId);
                }
                catch { }
            }

            await SendFirmError(session, "<b>Неверный формат!</b>\n\nПопробуйте еще раз.");
            return;
        }

        if (update.Message.Document != null)
        {
            var doc = update.Message.Document;
            bool isPng = doc.MimeType == "image/png" ||
                         (doc.FileName?.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ?? false);

            if (!isPng)
            {
                await _bot.DeleteMessage(session.ChatId, update.Message.MessageId);

                if (session.FirmsState.LastFirmsMessageId != 0)
                {
                    try { await _bot.DeleteMessage(session.ChatId, session.FirmsState.LastFirmsMessageId); }
                    catch { }
                }

                await SendFirmError(session, "<b>Неверный формат!</b>\n\nПришлите изображение в формате <b>PNG</b>.\n\nПопробуйте еще раз.");
                return;
            }
        }

        string fileId = update.Message.Photo != null
                        ? update.Message.Photo[^1].FileId
                        : update.Message.Document.FileId;

        var pathToImage = Path.Combine(
            AppContext.BaseDirectory,
            PathService.PHOTOS_PATH,
            $"{Guid.NewGuid()}.png");

        if (!await SavePhotoAsync(fileId, pathToImage))
        {
            await _bot.DeleteMessage(session.ChatId, update.Message.MessageId);

            if (session.FirmsState.LastFirmsMessageId != 0)
            {
                try
                {
                    await _bot.DeleteMessage(session.ChatId, session.FirmsState.LastFirmsMessageId);
                }
                catch { }
            }

            await SendFirmError(session, "<b>Произошла ошибка!</b>\n\nПопробуйте еще раз.");
            return;
        }

        if (session.FirmsState.LastFirmsMessageId != 0)
        {
            try
            {
                await _bot.DeleteMessage(session.ChatId, session.FirmsState.LastFirmsMessageId);
            }
            catch { }
        }

        try
        {
            await _bot.DeleteMessage(session.ChatId, update.Message.MessageId);
        }
        catch { }

        string outputPath = Path.Combine(
            AppContext.BaseDirectory,
            PathService.IMAGES_RESULTS_PATH,
            $"{Guid.NewGuid()}.png");

        string stampPath = Path.Combine(
            AppContext.BaseDirectory,
            PathService.STAMPS_PATH,
            session.FirmsState.Stamp + ".PNG");

        PictureService.MergePictures(stampPath, pathToImage, outputPath, signScale: 2);
        await using var stream = File.OpenRead(outputPath);
        var inputFile = InputFile.FromStream(stream);
        var fileName = $"{session.FirmsState.Stamp}_{session.ChatId}_{DateTime.UtcNow:dd.MM.yyyy}_Сделано_с_@ProtoKotBot.png";

        await _bot.SendDocument(
              chatId: session.ChatId,
              document: InputFile.FromStream(stream, fileName),
              replyMarkup: _keyboardFactory.FirstQuestionMenu,
              caption: "<b>Ваше готовое изображение.</b>\n\n<b>🤖 Сделано с помощью ПротоКот:</b>\n@TheProtoKot",
              parseMode: ParseMode.Html);

        if (File.Exists(outputPath))
            File.Delete(outputPath);

        if (File.Exists(pathToImage))
            File.Delete(pathToImage);

        session.SessionState.Status = UserSessionStatus.None;
    }

    private async Task SendFirmError(UserSession session, string text)
    {
        if (session.FirmsState.LastFirmsMessageId != 0)
        {
            try
            {
                await _bot.DeleteMessage(session.ChatId, session.FirmsState.LastFirmsMessageId);
            }
            catch { }
        }

        await using var stream = File.OpenRead(
            Path.Combine(AppContext.BaseDirectory, "assets", "firms.jpg"));

        var inputFile = InputFile.FromStream(stream);

        session.FirmsState.LastFirmsMessageId = (await _bot.SendPhoto(
            chatId: session.ChatId,
            photo: inputFile,
            caption: text,
            replyMarkup: _keyboardFactory.BlacklistMenu,
            parseMode: ParseMode.Html)).MessageId;
    }

    private async Task HandleBackToQuestionAsync(UserSession session, Update update)
    {
        var text = update?.Message?.Text;
        if (text == "⏪ Вернуться к предыдущему вопросу") return;

        var path = PathService.GetResultPathDocxResults(session.DocumentState.Fraction, session.ChatId, session.DocumentState.DocumentType);

        if (!DocumentService.ReplaceTextWithKey(path, session.LastMessage, text, session))
        {
            await SendMessageAsync("❌ Ошибка при записи шаблона.", session.ChatId);
            return;
        }

        session.SessionState.Status = UserSessionStatus.FillingQuestions;
        session.DocumentState.Step--;
        session.LastMessage = text!;

        await _bot.DeleteMessage(session.ChatId, update.Message.MessageId);
        _sessionManager.UpdateSession(session);
    }

    private async Task HandleQuestionsAsync(UserSession session, Update update)
    {
        var questions = await FileService.GetQuestions(session.DocumentState.Fraction, session.DocumentState.DocumentType);

        if (!await ProcessUpdateAsync(session, update, questions))
            return;

        if (session.DocumentState.Step >= questions.Length - 1)
        {
            await FinalizeQuestionsAsync(session, update, questions);
        }
        else
        {
            await MoveToNextQuestionAsync(session, update, questions);
        }
    }
}
