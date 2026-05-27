using OfficeOpenXml;
using System.Text;
using System.Text.RegularExpressions;

internal static class BlacklistPlayersService
{
    public static List<BlacklistTable> BlackListsTables { get; set; } = new List<BlacklistTable>();
    public static Dictionary<string, BlockedPlayers> BlockedPlayersPerServer { get; set; } = new Dictionary<string, BlockedPlayers>();

    public static List<string> GetBlockedPlayerPages(string playerName, string server)
    {
        var players = BlockedPlayersPerServer[server].Players
                       .Where(p => string.Equals(AlignmentMapper.RemoveParenthesesContent(p.Name), playerName, StringComparison.OrdinalIgnoreCase))
                       .ToList();

        if (players.Count == 0)
            return new List<string> { "Игрок не найден." };

        var pages = new List<string>();
        var currentPage = 1;

        foreach (var player in players)
        {
            var page = new StringBuilder();
            page.AppendLine(FormatBlockedPlayer(player, currentPage++, addMainTitle: pages.Count == 0));
            pages.Add(page.ToString().Trim());
        }

        return pages;
    }

    public static bool IsValidNickname(string nickname)
    {
        if (string.IsNullOrEmpty(nickname))
            return false;

        var pattern = @"^[A-Z][a-z]*_[a-zA-Z]+$";
        return Regex.IsMatch(nickname, pattern);
    }

    public static bool IsPlayerBlocked(string playerName, string server)
    {
        try
        {
          return BlockedPlayersPerServer[server]
                .Players
                .Any(p => string.Equals(AlignmentMapper.RemoveParenthesesContent(p.Name), playerName, StringComparison.OrdinalIgnoreCase));

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }

    public static BlockedPlayers ExtractBlockedPlayersPerServer(string serverName, string tablePath)
    {
        using var package = new ExcelPackage(new FileInfo(tablePath));
        var values = new BlockedPlayers();

        foreach (var sheet in package.Workbook.Worksheets)
        {
            if (sheet.Dimension == null)
                continue;

            var rows = sheet.Dimension.Rows;
            var cols = sheet.Dimension.Columns;

            var nameInfo = ExtractColumnAndRowForNames(rows, cols, sheet);
            int headerRow = nameInfo.row;
            int targetColNames = nameInfo.col;
            int targetColReporter = ExtractColumnForReporter(rows, cols, headerRow, sheet);
            int targetColReason = ExtractColumnForReason(rows, cols, headerRow, sheet);
            // int targetColVK = ExtractColumnForVK(rows, cols, sheet);
            int targetColStatus = ExtractColumnForStatus(rows, cols, headerRow, sheet);
            int targetColDateEntry = ExtractColumnForDateEntry(rows, cols, headerRow, sheet);
            int targetColDateEnd = ExtractColumnForDateEnd(rows, cols, headerRow, sheet);
            int targetColMultipleAccounts = ExtractColumnForMA(rows, cols, headerRow, sheet);
            int targetColExtender = ExtractColumnForExtender(rows, cols, headerRow, sheet);

            try
            {
                for (int r = headerRow + 1; r <= rows; r++)
                {
                    var playerName = sheet.Cells[r, targetColNames].Text;

                    if (string.IsNullOrEmpty(playerName))
                        continue;

                    var player = new BlockedPlayer();

                    player.SheetRow = r;
                    player.Sheet = sheet.Name;

                    if (targetColNames != -1)
                        player.Name = playerName;

                    if (targetColReporter != -1 && !string.IsNullOrEmpty(sheet.Cells[r, targetColReporter].Text))
                        player.Reporter = sheet.Cells[r, targetColReporter].Text;

                    if (targetColReason != -1 && !string.IsNullOrEmpty(sheet.Cells[r, targetColReason].Text))
                        player.Reason = sheet.Cells[r, targetColReason].Text;

                    // if (targetColVK != -1 && !string.IsNullOrEmpty(sheet.Cells[r, targetColVK].Text))
                    //      player.VK = sheet.Cells[r, targetColVK].Text;

                    if (targetColStatus != -1 && !string.IsNullOrEmpty(sheet.Cells[r, targetColStatus].Text))
                        player.Status = sheet.Cells[r, targetColStatus].Text;

                    if (targetColDateEntry != -1 && !string.IsNullOrEmpty(sheet.Cells[r, targetColDateEntry].Text))
                        player.DateEntry = sheet.Cells[r, targetColDateEntry].Text;

                    if (targetColDateEnd != -1 && !string.IsNullOrEmpty(sheet.Cells[r, targetColDateEnd].Text))
                        player.DateEnd = sheet.Cells[r, targetColDateEnd].Text;

                    if (targetColMultipleAccounts != -1 && !string.IsNullOrEmpty(sheet.Cells[r, targetColMultipleAccounts].Text))
                        player.MultipleAccounts = sheet.Cells[r, targetColMultipleAccounts].Text;

                    if (targetColExtender != -1 && !string.IsNullOrEmpty(sheet.Cells[r, targetColExtender].Text))
                        player.Extender = sheet.Cells[r, targetColExtender].Text;

                    values.Players.Add(player);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        return values;
    }

    private static (int row, int col) ExtractColumnAndRowForNames(int rows, int cols, ExcelWorksheet sheet)
    {
        int targetCol = -1;
        int headerRow = -1;

        for (int r = 1; r <= rows; r++)
        {
            for (int c = 1; c <= cols; c++)
            {
                if (BlockedPlayersConstants.PlayerNamePatterns.Contains(sheet.Cells[r, c].Text))
                {
                    targetCol = c;
                    headerRow = r;
                    return (headerRow, targetCol);
                }
            }
        }

        return (headerRow, targetCol);
    }

    private static int ExtractColumnForReporter(int rows, int cols, int headerRow, ExcelWorksheet sheet)
    {
        for (int r = headerRow; r <= rows; r++)
            for (int c = 1; c <= cols; c++)
                if (BlockedPlayersConstants.ReporterPatterns.Contains(sheet.Cells[r, c].Text))
                    return c;

        return -1;
    }

    private static int ExtractColumnForReason(int rows, int cols, int headerRow, ExcelWorksheet sheet)
    {
        for (int r = headerRow; r <= rows; r++)
            for (int c = 1; c <= cols; c++)
                if (BlockedPlayersConstants.ReasonPatterns.Contains(sheet.Cells[r, c].Text))
                    return c;

        return -1;
    }

    /*
    private static int ExtractColumnForVK(int rows, int cols, ExcelWorksheet sheet)
    {
        for (int r = 1; r <= rows; r++)
            for (int c = 1; c <= cols; c++)
                if (BlockedPlayersConstants.VKPatterns.Contains(sheet.Cells[r, c].Text))
                    return c;

        return -1;
    }
    */

    private static int ExtractColumnForDateEntry(int rows, int cols, int headerRow, ExcelWorksheet sheet)
    {
        for (int r = headerRow; r <= rows; r++)
            for (int c = 1; c <= cols; c++)
                if (BlockedPlayersConstants.DateEntryPatterns.Contains(sheet.Cells[r, c].Text))
                    return c;

        return -1;
    }

    private static int ExtractColumnForDateEnd(int rows, int cols, int headerRow, ExcelWorksheet sheet)
    {
        for (int r = headerRow; r <= rows; r++)
            for (int c = 1; c <= cols; c++)
                if (BlockedPlayersConstants.DateEndPatterns.Contains(sheet.Cells[r, c].Text))
                    return c;

        return -1;
    }

    private static int ExtractColumnForStatus(int rows, int cols, int headerRow, ExcelWorksheet sheet)
    {
        for (int r = headerRow; r <= rows; r++)
            for (int c = 1; c <= cols; c++)
                if (BlockedPlayersConstants.StatusPatterns.Contains(sheet.Cells[r, c].Text))
                    return c;

        return -1;
    }

    private static int ExtractColumnForMA(int rows, int cols, int headerRow, ExcelWorksheet sheet)
    {
        for (int r = headerRow; r <= rows; r++)
            for (int c = 1; c <= cols; c++)
                if (BlockedPlayersConstants.MultipleAccountsPatterns.Contains(sheet.Cells[r, c].Text))
                    return c;

        return -1;
    }

    private static int ExtractColumnForExtender(int rows, int cols, int headerRow, ExcelWorksheet sheet)
    {
        for (int r = headerRow; r <= rows; r++)
            for (int c = 1; c <= cols; c++)
                if (BlockedPlayersConstants.ExtenderPatterns.Contains(sheet.Cells[r, c].Text))
                    return c;

        return -1;
    }

    private static string FormatBlockedPlayer(BlockedPlayer p, int pageNumber, bool addMainTitle = true)
    {
        if (p == null)
            return "Игрок не найден.";

        string Safe(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : System.Net.WebUtility.HtmlEncode(value);

        if (addMainTitle)
        {
            return
        $@"<b>🚫 Информация о блокировке игрока</b>

<b>Страница {pageNumber}:</b>

<b>👤 Ник:</b> {Safe(p.Name, "Не указан")}
<b>📋 Лист:</b> {Safe(p.Sheet, "Не указан")}
<b>📌 Строка:</b> {(p.SheetRow?.ToString() ?? "Не указана")}

<b>🧑‍⚖️ Кем занесён:</b> {Safe(p.Reporter, "Не указан")}
<b>📄 Причина:</b> {Safe(p.Reason, "Не указана")}
<b>📊 Статус:</b> {Safe(p.Status, "Не указан")}

<b>📅 Дата выдачи:</b> {Safe(p.DateEntry, "Не указана")}
<b>⏳ Дата окончания:</b> {Safe(p.DateEnd, "Не указана")}

<b>🧩 Твинки:</b> {Safe(p.MultipleAccounts, "Не указан")}";
        }
        else
        {
            return
$@"<b>Страница {pageNumber}:</b>

<b>👤 Ник:</b> {Safe(p.Name, "Не указан")}
<b>📋 Лист:</b> {Safe(p.Sheet, "Не указан")}
<b>📌 Строка:</b> {(p.SheetRow?.ToString() ?? "Не указана")}

<b>🧑‍⚖️ Кем занесён:</b> {Safe(p.Reporter, "Не указан")}
<b>📄 Причина:</b> {Safe(p.Reason, "Не указана")}
<b>📊 Статус:</b> {Safe(p.Status, "Не указан")}

<b>📅 Дата выдачи:</b> {Safe(p.DateEntry, "Не указана")}
<b>⏳ Срок:</b> {Safe(p.DateEnd, "Не указана")}

<b>🧩 Твинки:</b> {Safe(p.MultipleAccounts, "Не указан")}";
        }
    }
}

