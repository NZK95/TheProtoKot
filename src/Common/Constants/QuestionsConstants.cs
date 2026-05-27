using AmazingBot;

internal static class QuestionsConstants
{
    public const string FIRST_QUESTION = "1. Введите день месяца от 1 до 31.";
    public const string LAST_QUESTION_SIGN = "Отправьте фотографию Вашей подписи (PNG).";
    public const string LAST_QUESTION_SHTAMP = "Отправьте фотографию Вашего штампа (PNG).";

    public static readonly List<string> DayQuestionsPatterns = new()
        {
            "1. Введите день месяца от 1 до 31",
            "Введите день месяца от 1 до 31",
            "Введите день месяца",
            "день месяца",
            "от 1 до 31"
        };

    public static readonly List<string> MonthQuestionsPatterns = new()
        {
            "Введите название месяца",
            "Введите названия месяца",
            "Введите номер месяца от 1 до 12",
            "Введите месяц"
        };

    public static readonly List<string> YearQuestionsPatterns = new()
        {
            "Введите четырехзначное число года"
        };

    public static readonly List<string> ImageQuestionsPatters = new()
        {
            "Отправьте фотографию",
            "Отправьте фотографию Вашего штампа (PNG).",
            "Отправьте фотографию Вашей подписи (PNG).",
        };

    public static readonly List<string> ImageKeys = new()
        {
            "{{image}}","{{image1}}","{{image2}}","{{image3}}"
        };
}
