using clrhost;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace clrhost
{
    internal static class DocumentService
    {
        public static bool ReplaceTextWithKey(string documentPath, string key, string value, UserSession session)
        {
            try
            {
                using var doc = DocX.Load(documentPath);

                doc.ReplaceText(
                key,
                value,
                false,
                System.Text.RegularExpressions.RegexOptions.None);

                var baseDir = AppContext.BaseDirectory;
                var userPath = PathService.GetResultPathDocxResults(session.DocumentState.Fraction, session.ChatId, session.DocumentState.DocumentType);

                if (documentPath == Path.Combine(baseDir, "templates", session.DocumentState.Fraction, $"{session.DocumentState.DocumentType}.docx"))
                    doc.SaveAs(userPath);
                else
                    doc.Save(documentPath);

                return true;
            }
            catch
            {
                return false;
            }
        }
        public static void RemoveImageKeysIfPresent(string documentPath, UserSession session)
        {
            try
            {
                using var doc = DocX.Load(documentPath);

                foreach (var key in QuestionsConstants.ImageKeys)
                {
                    doc.ReplaceText(
                    key,
                    string.Empty,
                    false,
                    System.Text.RegularExpressions.RegexOptions.None);
                }

                var baseDir = AppContext.BaseDirectory;
                var userPath = PathService.GetResultPathDocxResults(session.DocumentState.Fraction, session.ChatId, session.DocumentState.DocumentType);

                if (documentPath == Path.Combine(baseDir, "templates", session.DocumentState.Fraction, $"{session.DocumentState.DocumentType}.docx"))
                    doc.SaveAs(userPath);
                else
                    doc.Save(documentPath);
            }
            catch { }
        }


        public static bool ReplaceImageWithKey(string documentPath, string imagePath, string key, Alignment aligment)
        {
            try
            {
                using var doc = DocX.Load(documentPath);

                var paragraph = doc.Paragraphs.First(p => p.Text.Contains(key));

                if (paragraph == null)
                    return false;

                paragraph.ReplaceText(key, "");

                paragraph.SetLineSpacing(LineSpacingType.Before, 0.0f);
                paragraph.SetLineSpacing(LineSpacingType.After, 0.0f);
                paragraph.KeepWithNextParagraph(false);
                paragraph.KeepLinesTogether(false);
                paragraph.Alignment = aligment;

                var image = doc.AddImage(imagePath);
                var picture = image.CreatePicture();

                picture.Width = 130;
                picture.Height = 130;

                paragraph.AppendLine();
                paragraph.AppendPicture(picture);

                doc.Save(documentPath);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public static bool IsSpecificDocument(string documentType)
        {
            return documentType == "Доверенность_Суд" ||
                documentType == "Протокол задержания" ||
                documentType == "Протокол личного досмотра";
        }
    }
}
