using clrhost;

internal static class FileService
{
    public static async Task<string[]> GetQuestions(string fraction, string documentType)
    {
        var lines = await File.ReadAllLinesAsync(Path.Combine(PathService.QUESTIONS_PATH, fraction, $"{documentType}.txt"));
        return lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
    }

    public static List<string> GetFileNames(string folderPath)
    {
        var result = new List<string>();

        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException("Папка не найдена");

        var files = Directory.GetFiles(folderPath);

        foreach (var file in files)
            result.Add(Path.GetFileName(file));

        return result;
    }

    public static void ClearDataAtStart()
    {
        foreach (var file in Directory.GetFiles(PathService.IMAGES_RESULTS_PATH))
            File.Delete(file);

        foreach (var file in Directory.GetFiles(PathService.PHOTOS_PATH))
            File.Delete(file);

        foreach (var file in Directory.GetFiles(PathService.DOCX_RESULTS_PATH))
            File.Delete(file);
    }
}
