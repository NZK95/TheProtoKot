using System.Text.Json.Serialization;

internal sealed class FirmsState
{
    [JsonIgnore] public int LastFirmsMessageId { get; set; } = 0;
    [JsonIgnore] public string Stamp { get; set; }

    public void Reset()
    {
        LastFirmsMessageId = 0;
        Stamp = string.Empty;
    }

    public override string ToString()
    {
        return $"Выбранный штамп: {Stamp}";
    }
}
