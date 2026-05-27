using System.Text.Json.Serialization;

internal sealed class Event
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Timezone { get; set; } = "Europe/Moscow";
    public List<ScheduleRule> Rules { get; set; } = new();
}
