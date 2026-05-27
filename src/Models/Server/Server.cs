using AmazingBot;
using System.Text;
using System.Text.Json.Serialization;
internal sealed class Server
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; }

    [JsonPropertyName("max_online")]
    public int MaxOnline { get; set; }

    [JsonPropertyName("port")]
    public string Port { get; set; }

    [JsonPropertyName("ip")]
    public string Ip { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("online")]
    public int Online { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("promoted")]
    public bool Promoted { get; set; }

    [JsonPropertyName("queue")]
    public int Queue { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("info")]
    public Info Info { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"{ServerService.GetServerEmoji(Name)} <b>{ServerService.Escape(Name)}</b> (#{Number})");
        sb.AppendLine($"👥 Онлайн: <b>{Online} / {MaxOnline}</b>");
        sb.AppendLine($"👣 Очередь: <b>{Queue}</b>");
        sb.AppendLine($"🔆 Акция: <b>{(Info.Boost == 1 ? "Нет акции" : $"x{Info.Boost}")}</b>");
        sb.AppendLine($"📡 IP: <code>{Ip}:{Port}</code>");
        sb.AppendLine($"🎚️ Статус: <b>{(ServerService.Escape(Status) == "PLAYABLE" ? "Активен ✅" : "Неактивен ❌")}</b>");

        return sb.ToString();
    }
}
