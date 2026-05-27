using clrhost;
using System.Text.Json.Serialization;

internal sealed class ReportedAccountRecord
{
    public long ChatId { get; set; }
    public string Username { get; set; } = "";
    public string Nickname { get; set; } = "";
    public long AccountId { get; set; }
    public string Server { get; set; } = "";
    public bool NotificationsEnabled { get; set; } = true;
}