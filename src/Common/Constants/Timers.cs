using clrhost;

internal static class Timers
{
    public static readonly int SERVERS_AUTO_UPDATE_TIME = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;
    public static readonly int BLACKLIST_DOWNLOADER_TIME = (int)TimeSpan.FromHours(4).TotalMilliseconds;
    public static readonly int SESSIONS_UPDATER_TIME = (int)TimeSpan.FromSeconds(30).TotalMilliseconds;
    public static readonly int EVENTS_TASK_TIME = (int)TimeSpan.FromSeconds(30).TotalMilliseconds;
    public static readonly int REPORT_CHECKER_UPDATE_TIME = (int)TimeSpan.FromSeconds(30).TotalMilliseconds;
}
