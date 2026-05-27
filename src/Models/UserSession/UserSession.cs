using clrhost;
using System.Text.Json.Serialization;

internal sealed class UserSession
{
    public long ChatId { get; set; }
    public string? TelegramUsername { get; set; } = null;
    public string? LastMessage { get; set; } = null;
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore] public int LastUpdateMessageId { get; set; } = 0;
    [JsonIgnore] public int LastCommandMessageId { get; set; } = 0;

    [JsonIgnore] public DocumentState DocumentState { get; set; } = new();
    [JsonIgnore] public SessionState SessionState { get; set; } = new();
    [JsonIgnore] public HintState HintState { get; set; } = new();
    [JsonIgnore] public ServersState ServersState { get; set; } = new();
    [JsonIgnore] public EventState EventState { get; set; } = new();
    [JsonIgnore] public FirmsState FirmsState { get; set; } = new();
    [JsonIgnore] public BlockedPlayersState BlockedPlayersState { get; set; } = new();
    [JsonIgnore] public ReportCheckerState ReportCheckerState { get; set; } = new();

    public void ClearData()
    {
        DocumentState.Reset(clearFiles: true);
        SessionState.Reset();
        HintState.Reset();
        BlockedPlayersState.Reset();
        FirmsState.Reset();
        ReportCheckerState.Reset();
    }

    public override string ToString()
    {
        return $"Имя: @{TelegramUsername} ({ChatId})\n" +
                $"Последнее сообщение: {LastMessage}";
    }
}
