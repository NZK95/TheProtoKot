internal static class BlockedPlayersConstants
{
    public const string SILVER_SPECIFIC_SERVER = "SILVER";

    public static readonly List<string> PlayerNamePatterns = new()
    {
           "Ник игрока",
           "Nick_Name игрока",
           "Игровой никнейм",
           "Никнейм игрока и номер аккаунта",
           "Игровое имя",
           "NickName игрока",
           "Никнейм игрока"
    };

    public static readonly List<string> ReporterPatterns = new()
    {
           "Кем был занесён",
           "Кем занесён",
           "Уполномоченный администратор",
           "Выдано",
    };

    public static readonly List<string> ReasonPatterns = new()
    {
           "Причина",
           "Причина внесения",
           "Причина занесения",
    };

    public static readonly List<string> MultipleAccountsPatterns = new()
        {
         "Твинки",
         "Распростроняется ли на твинки?"
    };

    public static readonly List<string> VKPatterns = new()
    {
        "ID VK",
        "Ссылка на VK",
        "VK (оригинальный)",
        "VK",
        "VK (оригинальный ID)",
    };

    public static readonly List<string> DateEntryPatterns = new()
    {
       "Дата внесения",
       "Дата внесения ЧС"
    };

    public static readonly List<string> DateEndPatterns = new()
    {
       "Дата окончания",
       "Дата окончания ЧС",
       "Дата вынесения",
       "Дата вынесения ЧС",
    };

    public static readonly List<string> StatusPatterns = new()
    {
        "Статус ЧС",
        "Статус",
        "Статус блокировки"
    };

    public static readonly List<string> ExtenderPatterns = new()
    {
        "Кто продлил"
    };
}
