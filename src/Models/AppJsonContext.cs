using System.Text.Json.Serialization;
using clrhost;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    UseStringEnumConverter = true
)]
[JsonSerializable(typeof(Event))]
[JsonSerializable(typeof(List<Event>))]
[JsonSerializable(typeof(Root))]
[JsonSerializable(typeof(DayType))]
[JsonSerializable(typeof(TimeWindow))]
[JsonSerializable(typeof(ScheduleRule))]

internal partial class AppJsonContext : JsonSerializerContext { }
