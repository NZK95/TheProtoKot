using System.Text.Json.Serialization;

internal sealed class ServersState
{
    public bool ServersUpdateEnabled { get; set; } = false;
    [JsonIgnore] public string ServerType { get; set; }
    [JsonIgnore] public string ServersMessageAutoUpdate { get; set; }
    [JsonIgnore] public int ServerMessageId { get; set; }
    [JsonIgnore] public int AlreadyUpdatingMessageId { get; set; }
    [JsonIgnore] public int NotSupportedMessageId { get; set; }
    [JsonIgnore] public int AutoUpdateStoppedMessageId { get; set; }

    public void StopServersAutoUpdate()
    {
        ServersUpdateEnabled = false;
        ServerType = ServersMessageAutoUpdate = string.Empty;
        ServerMessageId = AlreadyUpdatingMessageId = NotSupportedMessageId = AutoUpdateStoppedMessageId = 0;
    }
}