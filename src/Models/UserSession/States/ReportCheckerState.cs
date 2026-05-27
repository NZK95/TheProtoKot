using System.Text.Json.Serialization;

namespace clrhost
{
    internal class ReportCheckerState
    {
        [JsonIgnore] public int Step { get; set; } = 0;
        [JsonIgnore] public long AccountID { get; set; } = 0;
        [JsonIgnore] public string Nickname { get; set; } = string.Empty;
        [JsonIgnore] public string Server { get; set; } = string.Empty;
        [JsonIgnore] public int ReportCheckerMessageId { get; set; } = 0;
        [JsonIgnore] public List<(string text, string url, string videos)> FoundReports { get; set; } = new();
        [JsonIgnore] public int CurrentReportPage { get; set; } = 0;
        [JsonIgnore] public int PageMessageId { get; set; } = 0;

        public void Reset()
        {
            Step = 0;
            AccountID = 0;
            Nickname = Server = string.Empty;
            CurrentReportPage = 0;
            FoundReports = new();
        }
    }
}
