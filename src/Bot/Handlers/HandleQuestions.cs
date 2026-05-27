using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using AmazingBot;

internal sealed partial class TelegramBot
{
    private async Task<bool> HandleTextUpdateAsync(UserSession session, Update update, string[] questions)
    {
        try
        {
            var userAnswer = update?.Message?.Text;
            session.LastMessage = userAnswer!;
            var currentQuestion = questions[session.DocumentState.Step ].Split('|')[0].Trim();
            var currentQuestionKey = questions[session.DocumentState.Step ].Split('|')[1].Trim();

            if (QuestionsConstants.ImageQuestionsPatters.Any(x => currentQuestion.Contains(x)))
            {
                return false;
            }

            var path = session.DocumentState.Step == 0
                ? PathService.GetTemplatePath(session.DocumentState.Fraction, session.DocumentState.DocumentType)
                : PathService.GetResultPathDocxResults(session.DocumentState.Fraction, session.ChatId, session.DocumentState.DocumentType);

            return DocumentService.ReplaceTextWithKey(path, currentQuestionKey, userAnswer, session);
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> HandleImageFileUpdateAsync(UserSession session, Update update, string[] questions)
    {
        try
        {
            var question = questions[session.DocumentState.Step ].Split('|')[0].Trim();
            var key = questions[session.DocumentState.Step ].Split('|')[1].Trim();
            var position = questions[session.DocumentState.Step ].Split('|')[2].Trim();

            if (DocumentService.IsSpecificDocument(session.DocumentState.DocumentType))
                return await HandleSpecificDocumentImageAsync(session, update, key, position);
            else
                return await HandleRegularDocumentImageAsync(session, update, question, key, position);
        }
        catch { return false; }
    }

    private async Task<bool> HandleRegularDocumentImageAsync(UserSession session, Update update, string question, string key, string position)
    {
        try
        {
            var fileId = update.Message.Document.FileId;
            var pathToImage = Path.Combine(PathService.PHOTOS_PATH, $"{Guid.NewGuid()}.png");
            var pathToResult = PathService.GetResultPathDocxResults(session.DocumentState.Fraction, session.ChatId, session.DocumentState.DocumentType);

            if (!await SavePhotoAsync(fileId, pathToImage))
                return false;

            if (question.RemoveLeadingNumber() == QuestionsConstants.LAST_QUESTION_SIGN)
            {
                session.DocumentState.PathToSign = pathToImage;

                if (!string.IsNullOrEmpty(session.DocumentState.PathToShtamp) && File.Exists(session.DocumentState.PathToShtamp))
                {
                    var pathToMergedPicture = PathService.GetMergedPicturePath(session.DocumentState.Fraction, session.ChatId);
                    session.DocumentState.PathToMerged = pathToMergedPicture;

                    if (!PictureService.MergePictures(session.DocumentState.PathToShtamp, session.DocumentState.PathToSign, pathToMergedPicture))
                        return false;

                    if (!DocumentService.ReplaceImageWithKey(documentPath: pathToResult, imagePath: pathToMergedPicture, key: key, aligment: AlignmentMapper.GetAlignment(position)))
                        return false;
                }
                else
                {
                    if (!DocumentService.ReplaceImageWithKey(documentPath: pathToResult, imagePath: pathToImage, key: key, aligment: AlignmentMapper.GetAlignment(position)))
                        return false;
                }

                _sessionManager.UpdateSession(session);
            }
            else
            {
                session.DocumentState.PathToShtamp = pathToImage;
                _sessionManager.UpdateSession(session);
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }

    private async Task<bool> HandleSpecificDocumentImageAsync(UserSession session, Update update, string key, string position)
    {
        try
        {
            var resultPath = PathService.GetResultPathDocxResults(session.DocumentState.Fraction, session.ChatId, session.DocumentState.DocumentType);
            var pathToImage = Path.Combine(PathService.PHOTOS_PATH, $"{Guid.NewGuid()}.png");
            var fileId = update.Message.Document.FileId;

            if (!await SavePhotoAsync(fileId, pathToImage))
                return false;

            if (session.DocumentState.PathToShtamp == "" && session.DocumentState.PathToSign == "")
            {
                session.DocumentState.PathToShtamp = pathToImage;
                _sessionManager.UpdateSession(session);
                return DocumentService.ReplaceImageWithKey(documentPath: resultPath, imagePath: session.DocumentState.PathToShtamp, key: key, aligment: AlignmentMapper.GetAlignment(position));
            }

            if (session.DocumentState.PathToSign == "")
            {
                session.DocumentState.PathToSign = pathToImage;
                _sessionManager.UpdateSession(session);
            }

            return DocumentService.ReplaceImageWithKey(documentPath: resultPath, imagePath: session.DocumentState.PathToShtamp, key: key, aligment: AlignmentMapper.GetAlignment(position));
        }
        catch
        {
            return false;
        }
    }

    private async Task SendImageQuestionAsync(UserSession session, string question)
    {
        var keyboard = new InlineKeyboardMarkup();
        keyboard.AddButton(new InlineKeyboardButton("У меня нет", "ImageQuestions@Нет-Картинки"));

        var sentMessage = await _bot.SendMessage(
            cancellationToken: _cts.Token,
            chatId: session.ChatId,
            text: DateService.AddDateToQuestion(question),
            parseMode: ParseMode.Html,
            replyMarkup: keyboard);

        session.DocumentState.LastQuestionMessageId = sentMessage.MessageId;
        _sessionManager.UpdateSession(session);
    }

    private async Task SendTextQuestionAsync(UserSession session, string question)
    {
        var formmatedQuestion = DateService.AddDateToQuestion(question);
        var inlineKeyboard = _keyboardFactory.BuildDateHintsMenu(question);

        var sentMessage = await _bot.SendMessage(
             cancellationToken: _cts.Token,
             chatId: session.ChatId,
             text: formmatedQuestion,
             parseMode: ParseMode.Html,
             replyMarkup: inlineKeyboard is null ? _keyboardFactory.QuestionsMenu : inlineKeyboard);

        session.DocumentState.LastQuestionMessageId = sentMessage.MessageId;
        _sessionManager.UpdateSession(session);
    }

    private async Task FinalizeQuestionsAsync(UserSession session, Update update, string[] questions)
    {
        if (session.DocumentState.DocumentMainTitleMessageId != 0 && session.DocumentState.DocumentMainTitleMessageId != null)
            await _bot.DeleteMessage(session.ChatId, session.DocumentState.DocumentMainTitleMessageId);

        var currentQuestion = questions[session.DocumentState.Step ];
        var (key, position) = ExtractQuestionMetadata(currentQuestion);
        var resultPath = PathService.GetResultPathDocxResults(session.DocumentState.Fraction, session.ChatId, session.DocumentState.DocumentType);

        await ProcessStampAsync(resultPath, session, key, position);
        DocumentService.RemoveImageKeysIfPresent(resultPath, session);
        await CleanupPreviousQuestionAsync(session, update);
        await SendResultFormatMenuAsync(session);
    }

    private async Task CleanupPreviousQuestionAsync(UserSession session, Update update)
    {
        bool isFromCallback = update.Type == UpdateType.CallbackQuery;

        try
        {
            if (session.DocumentState.LastQuestionMessageId != 0 && session.DocumentState.LastQuestionMessageId != null)
                await _bot.DeleteMessage(session.ChatId, session.DocumentState.LastQuestionMessageId);

            if (!isFromCallback && update.Message != null)
                await _bot.DeleteMessage(session.ChatId, update.Message.Id);
        }
        catch
        {
            await SendMessageAsync(
                "❌ Произошла ошибка.\nПожалуйста, попробуйте ещё раз.",
                session.ChatId);
        }
    }

    private async Task MoveToNextQuestionAsync(UserSession session, Update update, string[] questions)
    {
        await CleanupPreviousQuestionAsync(session, update);

        session.DocumentState.Step ++;
        var nextQuestion = questions[session.DocumentState.Step ].Split('|')[0];
        var formattedQuestion = nextQuestion.RemoveLeadingNumber();

        if (IsImageQuestion(formattedQuestion))
        {
            await SendImageQuestionAsync(session, nextQuestion);
        }
        else
        {
            await SendTextQuestionAsync(session, nextQuestion);
        }
    }

    private async Task BackToPreviousQuestionAsync(UserSession session, Update update)
    {
        try
        {
            if (session.DocumentState.Step == 0)
                return;

            if(session.DocumentState.LastQuestionMessageId != 0 && session.DocumentState.LastQuestionMessageId != null)
                await _bot.DeleteMessage(session.ChatId, session.DocumentState.LastQuestionMessageId);

            var questions = await FileService.GetQuestions(session.DocumentState.Fraction, session.DocumentState.DocumentType);
            var previousQuestion = questions[session.DocumentState.Step - 1].Split('|')[0].Trim() + $"\n\n<b>⚠️ Введите ответ 2 раза для подтверждения.</b>";
            var sentMessage = await _bot.SendMessage(
                cancellationToken: _cts.Token,
                chatId: session.ChatId,
                text: previousQuestion,
                parseMode: ParseMode.Html,
                replyMarkup: _keyboardFactory.QuestionsMenu
            );

            session.DocumentState.LastQuestionMessageId = sentMessage.MessageId;
            session.SessionState.Status = UserSessionStatus.BackingToPreviousQuestion;
            _sessionManager.UpdateSession(session);
        }
        catch
        {
            await SendMessageAsync("❌ <b>Произошла ошибка.</b>\nПожалуйста, попробуйте ещё раз.", session.ChatId);
        }
    }

    private string GetCurrentQuestion(string[] questions, int step)
    {
        return questions[step].Split('|')[0].Trim();
    }

    private (string key, string position) ExtractQuestionMetadata(string question)
    {
        var parts = question.Split('|');
        return (
            key: parts.Length > 1 ? parts[1].Trim() : "",
            position: parts.Length > 2 ? parts[2].Trim() : ""
        );
    }

    private bool IsImageQuestion(string question)
    {
        return QuestionsConstants.ImageQuestionsPatters.Any(x =>
            question.Contains(x, StringComparison.OrdinalIgnoreCase));
    }
}
