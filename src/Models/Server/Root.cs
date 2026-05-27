using System.Text.Json.Serialization;

internal sealed class Root
{
    [JsonPropertyName("code")]
    public string Code { get; set; }

    [JsonPropertyName("response")]
    public Response Response { get; set; }
}