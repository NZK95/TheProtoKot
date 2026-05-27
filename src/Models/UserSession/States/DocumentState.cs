using System.Text.Json.Serialization;

internal sealed class DocumentState
{
    [JsonIgnore] public string Fraction { get; set; } = string.Empty;
    [JsonIgnore] public string DocumentType { get; set; } = string.Empty;
    [JsonIgnore] public int Step { get; set; } = 0;

    [JsonIgnore] public string PathToShtamp { get; set; } = string.Empty;
    [JsonIgnore] public string PathToResult { get; set; } = string.Empty;
    [JsonIgnore] public string PathToSign { get; set; } = string.Empty;
    [JsonIgnore] public string PathToMerged { get; set; } = string.Empty;

    [JsonIgnore] public int DocumentMainTitleMessageId { get; set; } = 0;
    [JsonIgnore] public int LastQuestionMessageId { get; set; } = 0;

    public void Reset(bool clearFiles = true)
    {
        if (clearFiles)
            ClearFiles();

        Fraction = DocumentType = string.Empty;
        Step = 0;
    }

    public void ClearFiles()
    {
        Delete(PathToShtamp);
        Delete(PathToSign);
        Delete(PathToMerged);
        Delete(PathToResult);

        PathToShtamp = PathToSign = PathToMerged = PathToResult = string.Empty;
    }

    private static void Delete(string path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
            File.Delete(path);
    }

    public override string ToString()
    {
        return    $"Фракция: {Fraction}\n"+
                  $"Тип документа: {DocumentType}\n" +
                  $"Вопрос: {Step + 1}";
    }
}
