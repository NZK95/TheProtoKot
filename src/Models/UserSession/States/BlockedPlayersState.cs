using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

internal sealed class BlockedPlayersState
{
    [JsonIgnore] public int LastBlockedPlayersMessageId { get; set; } = 0;
    [JsonIgnore] public List<string> Pages { get; set; } = new();
    [JsonIgnore] public string PlayerName { get; set; }
    [JsonIgnore] public string Server { get; set; }
    [JsonIgnore] public int CurrentPage { get; set; } = 0;

    public void Reset()
    {
        PlayerName = Server = string.Empty;
        Pages = new List<string>();
    }

    public override string ToString()
    {
        return $"В поиске: {PlayerName}\n" + 
                $"На сервере: {Server}";
    }
}