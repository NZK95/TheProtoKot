using AmazingBot;
using Spire.Doc.Documents;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

internal sealed partial class TelegramBot
{
    private async Task SendResultFormatMenuAsync(UserSession session)
    {
        var resultMessage = await _bot.SendMessage(
            cancellationToken: _cts.Token,
            chatId: session.ChatId,
            text: "Выберите в каком формате вы хотите получить результат.",
            parseMode: ParseMode.Html,
            replyMarkup: _keyboardFactory.ResultFormatChoiceMenu);

        session.DocumentState.LastQuestionMessageId = resultMessage.MessageId;
        session.SessionState.Status = UserSessionStatus.None;
        _sessionManager.UpdateSession(session);
    }

    private async Task SendResultInDocumentAsync(UserSession session)
    {
        try
        {
            var fileName = $"{session.DocumentState.DocumentType}_{DateTime.UtcNow:dd.MM.yyyy}_Сделано_с_@ProtoKotBot.docx";
            var resultPath = PathService.GetResultPathDocxResults(session.DocumentState.Fraction, session.ChatId, session.DocumentState.DocumentType);
            await using var stream = File.OpenRead(resultPath);

            await _bot.SendDocument(
                chatId: session.ChatId,
                document: InputFile.FromStream(stream, fileName),
                replyMarkup: _keyboardFactory.FirstQuestionMenu, 
                caption: "<b>Ваш готовый документ.</b>\n\n<b>🤖 Сделано с помощью ПротоКот:</b>\n@TheProtoKot",
                parseMode: ParseMode.Html);
        }
        catch (Exception ex)
        {
            {
                Console.WriteLine(ex.Message);
                await SendMessageAsync("❌ <b>Произошла ошибка.</b>", session.ChatId);
            }
        }
    }

    private async Task SendResultInImagesAsync(UserSession session)
    {
        try
        {
            var resultPath = PathService.GetResultPathDocxResults(session.DocumentState.Fraction, session.ChatId, session.DocumentState.DocumentType);
            var doc = new Spire.Doc.Document();
            doc.LoadFromFile(resultPath);

            for (int i = 0; i < doc.PageCount; i++)
            {
                var image = doc.SaveToImages(i, ImageType.Bitmap);

                using var highResImage = new Bitmap(2480, 3508);
                highResImage.SetResolution(300, 300);

                using (var g = Graphics.FromImage(highResImage))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.DrawImage(image, 0, 0, 2480, 3508);
                }

                var imageName = $"{session.DocumentState.DocumentType}_Страница_{i + 1}_{DateTime.UtcNow:dd.MM.yyyy}_@ProtoKotBot.png";
                var imagePath = Path.Combine(PathService.IMAGES_RESULTS_PATH, imageName);

                highResImage.Save(imagePath, ImageFormat.Png);

                await using (var stream = File.OpenRead(imagePath))
                {
                    await _bot.SendDocument(
                        chatId: session.ChatId,
                        document: InputFile.FromStream(stream, imageName),
                        parseMode: ParseMode.Html,
                        caption: $"<u>- Страница {i + 1}</u>"
                    );
                }

                image.Dispose();
                if (File.Exists(imagePath))
                    File.Delete(imagePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            await SendMessageAsync($"❌ Произошла ошибка при отправке документа.", session.ChatId);
        }
    }

    private async Task SendResultInLinks(UserSession session)
    {
        try
        {
            var resultPath = PathService.GetResultPathDocxResults(session.DocumentState.Fraction, session.ChatId, session.DocumentState.DocumentType);
            var doc = new Spire.Doc.Document();
            doc.LoadFromFile(resultPath);

            using var client = new HttpClient();

            for (int i = 0; i < doc.PageCount; i++)
            {
                var image = doc.SaveToImages(i, ImageType.Bitmap);

                using var highResImage = new Bitmap(2480, 3508);
                highResImage.SetResolution(300, 300);

                using (var g = Graphics.FromImage(highResImage))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.DrawImage(image, 0, 0, 2480, 3508);
                }

                var imageName = $"page_{i + 1}.png";
                var imagePath = Path.Combine(PathService.IMAGES_RESULTS_PATH, imageName);
                highResImage.Save(imagePath, ImageFormat.Png);

                var imageBytes = await File.ReadAllBytesAsync(imagePath);

                var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(imageBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                content.Add(fileContent, "image", imageName);

                var response = await client.PostAsync($"https://api.imgbb.com/1/upload?key={_imageServiceApiToken}", content);
                var jsonResult = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    await SendMessageAsync($"❌ Ошибка загрузки страницы {i + 1}", session.ChatId);
                }
                else
                {
                    using var docJson = JsonDocument.Parse(jsonResult);
                    var imageUrl = docJson.RootElement.GetProperty("data").GetProperty("image").GetProperty("url").GetString();

                    var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                    new[] { InlineKeyboardButton.WithUrl("Открыть 🌐", imageUrl) }
                });

                    await SendMessageAsync(
                        $"✅ <b>Страница {i + 1}/{doc.PageCount}</b>\n\n<code>{imageUrl}</code>",
                        session.ChatId,
                        inlineKeyboard
                    );
                }

                image.Dispose();
                if (File.Exists(imagePath))
                    File.Delete(imagePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            await SendMessageAsync($"❌ Ошибка при отправке.", session.ChatId);
        }
    }
}