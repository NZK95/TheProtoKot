namespace AmazingBot
{
    internal static class PathService
    {
        public static readonly string BASE_PATH = AppContext.BaseDirectory;
        public static readonly string TEMPLATES_PATH = Path.Combine(BASE_PATH, "templates");
        public static readonly string IMAGES_RESULTS_PATH = Path.Combine(BASE_PATH, "images-results");
        public static readonly string DOCX_RESULTS_PATH = Path.Combine(BASE_PATH, "docx-results");
        public static readonly string INSTRUCTIONS_PATH = Path.Combine(BASE_PATH, "instructions");
        public static readonly string QUESTIONS_PATH = Path.Combine(BASE_PATH, "questions");
        public static readonly string PHOTOS_PATH = Path.Combine(BASE_PATH, "photos");
        public static readonly string HINTS_PATH = Path.Combine(BASE_PATH, "hints");
        public static readonly string EVENTS_PATH = Path.Combine(BASE_PATH, "eventsDb", "events.json");
        public static readonly string BLACKLISTS_TABLES_PATH = Path.Combine(BASE_PATH, "tables");
        public static readonly string STAMPS_PATH = Path.Combine(BASE_PATH, "stamps");
        public static readonly string ADMIN_PATH = Path.Combine(BASE_PATH, "admin");

        public static string GetResultPathDocxResults(string fraction, long chatId, string documentType)
        {
            return Path.Combine(BASE_PATH, "docx-results", $"result - {chatId} - {fraction} - {documentType}.docx");
        }

        public static string GetTemplatePath(string fraction, string documentType)
        {
            return Path.Combine(BASE_PATH, "templates", fraction, $"{documentType}.docx");
        }

        public static string GetMergedPicturePath(string fraction, long chatId)
        {
            return Path.Combine(AppContext.BaseDirectory, "photos", $"merged-picture-{chatId}-{fraction}.png");
        }

        public static string GetInstructionPath(string fraction, string documentType)
        {
            return Path.Combine(INSTRUCTIONS_PATH, fraction, $"{documentType}.jpg");
        }
    }
}
