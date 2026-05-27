using System.Text.Json.Serialization;

internal sealed class Info
{
    [JsonPropertyName("boost")]
    public int Boost { get; set; }
}