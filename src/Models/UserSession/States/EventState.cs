
using System.Text.Json.Serialization;

internal sealed class EventState
{
    [JsonIgnore] public int LastEventMessageId { get; set; } = 0;
    [JsonIgnore] public int LastShowSubscribedMessageId { get; set; } = 0;
    [JsonIgnore] public int LastSubscribeToEventMessageId { get; set; } = 0;
    [JsonIgnore] public int LastUnscribeFromEventMessageId { get; set; } = 0;
}
