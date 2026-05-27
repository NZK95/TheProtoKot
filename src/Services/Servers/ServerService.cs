using clrhost;
using System.Net;
using System.Text;
using System.Text.Json;

internal static class ServerService
{
    public static async Task<List<Server>> GetServersAsync(string serversApiUrl)
    {
        try
        {
            using HttpClient client = new();
            var response = await client.GetStringAsync(serversApiUrl);

            var root = JsonSerializer.Deserialize(response, AppJsonContext.Default.Root);
            return root?.Response?.Servers;
        }
        catch
        {
            return new List<Server>();
        }
    }

    public static async Task<string> GetServerInfoMessage(string data, string serversApiUrl)
    {
        var servers = await GetServersAsync(serversApiUrl);

        if (data == "ВСЕ")
        {
            var message = new StringBuilder();

            message.AppendLine("📋 <b>Информация о серверах:</b>\n");
            foreach (var server in servers)
                message.AppendLine(server.ToString());

            message.AppendLine("⚠️ Автообновление для всех серверов не доступно!");
            return message.ToString();
        }
        else
        {
            var message = servers.Where(server => server.Name == data)
                                 .Select(server => server.ToString())
                                 .FirstOrDefault();

            return message ?? "Сервер не найден.";
        }
    }


    public static string GetServerEmoji(string Name)
    {
        return Name?.ToUpperInvariant() switch
        {
            "RED" => "❤️",
            "YELLOW" => "💛",
            "GREEN" => "💚",
            "AZURE" => "💙",
            "SILVER" => "🩶",
            "ROSE" => "🩷",
            "BLACK" => "🖤",
            "SKY" => "🩵",
            "TITAN" => "💜",
            "X" => "💗",
            "FIRE" => "🧡",
            _ => "🖥️"
        };
    }

    public static string Escape(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}