using System.Text.Json.Serialization;

internal sealed class HintState
{
    [JsonIgnore] public string HintName { get; set; }
    [JsonIgnore] public string HintsFromServer { get; set; }
    [JsonIgnore] public int LastHintMessageId { get; set; } = 0;

    public void Reset()
    {
        HintName = HintsFromServer = string.Empty;
    }
}
