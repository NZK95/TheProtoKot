using clrhost;
using System.Text.Json.Serialization;

internal sealed class Response
{
    [JsonPropertyName("servers")]
    public List<Server> Servers { get; set; }
}
