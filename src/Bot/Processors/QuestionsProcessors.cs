using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using clrhost;

internal sealed partial class TelegramBot
{
    private async Task ProcessStateAsync(ITelegramBotClient client, Update update, UserSession session)
    {
        if (update.Type == UpdateType.Message && update.Message.Text != null)
        {
            session.LastMessage = update?.Message?.Text ?? "N/A";
            _sessionManager.UpdateSession(session);

            if (!await BlockStateIfOccupied(update, session))
                return;

            await HandleUserMessageAsync(update?.Message?.Text!, update, session, client);
        }

        if (session.SessionState.Status != UserSessionStatus.None)
        {
            await HandleStatusAsync(session, update);
            return;
        }

        if (update.Type == UpdateType.CallbackQuery && update?.CallbackQuery != null)
        {
            _sessionManager.UpdateSession(session);
            await HandleCallbackQueryAsync(session, update, client);
        }

        _sessionManager.UpdateSession(session);
    }

    private async Task<bool> BlockStateIfOccupied(Update update, UserSession session)
    {
        if (session.SessionState.Status != UserSessionStatus.None &&
           BlockedCommandsDuringOperations.Any(x => update.Message.Text.Contains(x)))
        {
            var messageId = (await _bot.SendMessage(
                session.ChatId,
                "🚫 <b>Эта команда не доступна во время выполнения другой операции. Пожалуйста, повторите ввод: </b>",
                ParseMode.Html,
                replyMarkup: _keyboardFactory.StartMenu)).MessageId;

            await Task.Delay(2500);
            await _bot.DeleteMessage(session.ChatId, messageId);
            await _bot.DeleteMessage(session.ChatId, update.Message.Id);
            return false;
        }

        return true;
    }

    private async Task<bool> ProcessUpdateAsync(UserSession session, Update update, string[] questions)
    {
        return update.Type switch
        {
            UpdateType.Message when update.Message?.Photo?.Length > 0
                => await ProcessPhotoAsync(session, update, questions),

            UpdateType.Message when update.Message?.Document != null
                => await ProcessDocumentAsync(session, update, questions),

            UpdateType.Message when update.Message != null
                => await ProcessTextAsync(session, update, questions),

            UpdateType.CallbackQuery
                => await ProcessCallbackAsync(update, session, questions),

            _ => await ProcessInvalidFormatAsync(session)
        };
    }

    private async Task ProcessStampAsync(string resultPath, UserSession session, string key, string position)
    {
        if (!string.IsNullOrEmpty(session.DocumentState.PathToShtamp) && string.IsNullOrEmpty(session.DocumentState.PathToSign))
        {
            if (!DocumentService.ReplaceImageWithKey(
                documentPath: resultPath,
                imagePath: session.DocumentState.PathToShtamp,
                key: key,
                aligment: AlignmentMapper.GetAlignment(position)))
            {
                await SendMessageAsync(
                    "❌ Произошла ошибка.\nПожалуйста, попробуйте ещё раз.",
                    session.ChatId);
            }
        }
    }

    private async Task<bool> ProcessPhotoAsync(UserSession session, Update update, string[] questions)
    {
        var currentQuestion = GetCurrentQuestion(questions, session.DocumentState.Step );

        if (!IsImageQuestion(currentQuestion))
        {
            await SendInvalidFormatAsync(session);
            return false;
        }

        await SendMessageAsync(
            "❌ Допустимы только изображения <code>.png</code>, отправленные как файл.",
            session.ChatId,
            null,
            _keyboardFactory.QuestionsMenu);

        return false;
    }

    private async Task<bool> ProcessDocumentAsync(UserSession session, Update update, string[] questions)
    {
        var document = update.Message.Document;

        if (!document.MimeType?.StartsWith("image/") == true)
        {
            await SendInvalidFormatAsync(session);
            return false;
        }

        var currentQuestion = GetCurrentQuestion(questions, session.DocumentState.Step );

        if (!IsImageQuestion(currentQuestion))
        {
            await SendInvalidFormatAsync(session);
            return false;
        }

        if (!await HandleImageFileUpdateAsync(session, update, questions))
        {
            await SendMessageAsync(
                "❌ Ошибка при записи изображения.",
                session.ChatId,
                null,
                _keyboardFactory.QuestionsMenu);

            return false;
        }

        return true;
    }

    private async Task<bool> ProcessTextAsync(UserSession session, Update update, string[] questions)
    {
        if (!await HandleTextUpdateAsync(session, update, questions))
        {
            await SendMessageAsync(
                "❌ Ошибка при записи шаблона.",
                session.ChatId,
                null,
                _keyboardFactory.QuestionsMenu);

            return false;
        }

        return true;
    }

    private async Task<bool> ProcessCallbackAsync(Update update, UserSession session, string[] questions)
    {
        await _bot.AnswerCallbackQuery(update.CallbackQuery.Id, string.Empty);

        var prefix = update?.CallbackQuery?.Data.Split('@')[0];

        switch (prefix)
        {
            case "ImageQuestions":
                break;

            case "CurrentDate":
                var question = questions[session.DocumentState.Step ].Split('|')[0].Trim();
                var questionKey = questions[session.DocumentState.Step ].Split('|')[1].Trim();
                var value = DateService.GetCurrentDateValueByQuestion(question);

                var path = session.DocumentState.Step == 0
                    ? PathService.GetTemplatePath(session.DocumentState.Fraction, session.DocumentState.DocumentType)
                    : PathService.GetResultPathDocxResults(session.DocumentState.Fraction, session.ChatId, session.DocumentState.DocumentType);

                return DocumentService.ReplaceTextWithKey(path, questionKey, value, session);

            default:
                break;
        }
        return true;
    }

    private async Task<bool> ProcessInvalidFormatAsync(UserSession session)
    {
        await SendInvalidFormatAsync(session);
        return false;
    }
}
