internal sealed class ScheduleRule
{
    public DayType DayType { get; set; }
    public List<TimeWindow> Times { get; set; } = new();
}
