using System.Text;
using System.Text.Json;
using AmazingBot;

internal static class EventService
{
    public static List<Event> Events { get; private set; } = new List<Event>();

    public static void LoadEvents()
    {
        if (!File.Exists(PathService.EVENTS_PATH))
        {
            Console.WriteLine("events.json not found");
            return;
        }

        try
        {
            var json = File.ReadAllText(PathService.EVENTS_PATH);
            Events = JsonSerializer.Deserialize(json, AppJsonContext.Default.ListEvent) ?? new List<Event>();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public static async Task<bool> IsUserSubscribedToAny(long chatId, EventsDatabase eventsDb)
    {
        var record = await eventsDb.GetUserAsync(chatId);

        if (record is null || string.IsNullOrEmpty(record.EventsIds))
            return false;

        var ids = GetUserEventsIds(record.EventsIds);

        var userEvents = Events
            .Where(e => ids.Contains(e.Id))
            .ToList();

        if (!userEvents.Any())
            return false;

        return true;
    }

    public static async Task<bool> IsUserSubscribedToEvent(EventsDatabase eventsDb, long chatId, string eventId)
    {
        var record = await eventsDb.GetUserAsync(chatId);

        if (record is null || string.IsNullOrEmpty(record.EventsIds))
            return false;

        var ids = record.EventsIds
       .Split(',', StringSplitOptions.RemoveEmptyEntries)
       .Select(x => x.Trim())
       .ToList();

        return ids.Contains(eventId);
    }

    public static async Task<string> BuildUserEventsMessage(EventsDatabase eventsDb, long chatId)
    {
        var record = await eventsDb.GetUserAsync(chatId);

        var ids = GetUserEventsIds(record.EventsIds);

        var userEvents = Events
            .Where(e => ids.Contains(e.Id))
            .ToList();

        if (!userEvents.Any())
            return "📭 <b>У вас пока нет подписанных событий / мероприятий</b>";

        var sb = new StringBuilder();
        sb.AppendLine("<b>📌 Ваши подписанные события / мероприятия:</b>\n");

        var count = 1;
        foreach (var ev in userEvents)
        {
            sb.AppendLine($"{count}. <i>{ev.Name}</i>");
            sb.AppendLine();

            foreach (var rule in ev.Rules)
            {
                var label = rule.DayType switch
                {
                    DayType.Daily => "📅 Каждый день",
                    DayType.Weekdays => "📅 Будни",
                    DayType.Weekends => "📅 Выходные",
                    DayType.OddDays => "📅 Нечётные дни",
                    DayType.EvenDays => "📅 Чётные дни",
                    _ => "📅 Другое"
                };

                sb.AppendLine(label + ":");

                foreach (var timeWindow in rule.Times)
                {
                    var time = timeWindow.Start;
                    sb.AppendLine($" • {time:hh\\:mm}");
                }

                sb.AppendLine();
            }

            var tz = TimeZoneInfo.FindSystemTimeZoneById(ev.Timezone);
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

            var nextTimes = ev.Rules
                .Where(r => DateService.IsDayMatch(r.DayType, nowLocal.Date))
                .SelectMany(r => r.Times.Select(t => nowLocal.Date + t.Start))
                .ToList();

            if (!nextTimes.Any(t => t > nowLocal))
            {
                nextTimes = ev.Rules
                    .SelectMany(r => r.Times.Select(t => nowLocal.Date.AddDays(1) + t.Start))
                    .ToList();
            }

            var nextEvent = nextTimes.Where(t => t > nowLocal).OrderBy(t => t).FirstOrDefault();

            if (nextEvent != default)
            {
                var timeUntil = nextEvent - nowLocal;

                string nextEventStr;
                if (timeUntil.TotalHours >= 1)
                    nextEventStr = $"через {(int)timeUntil.TotalHours}ч {timeUntil.Minutes}м";
                else
                    nextEventStr = $"через {timeUntil.Minutes} мин";

                sb.AppendLine($"➡️ До следующего: {nextEvent:HH:mm} ({nextEventStr})");
            }

            sb.AppendLine("─────────────");
            count++;
        }

        return sb.ToString();
    }

    public static string BuildSubscribeEventMessage(Event ev)
    {
        return $@"
✅ <b>Подписка на {ev.Name} активирована!</b>

📢 Вы будете получать уведомления:
   ⏰ За 10 минут 
   ⏰ За 5 минут
   ⏰ За 1 минуты
   🚨 Когда началось

".Trim();
    }

    public static async Task RemoveUserSessionEvent(long chatId, EventsDatabase eventsDb, string eventId)
    {
        var record = await eventsDb.GetUserAsync(chatId);

        var ids = record.EventsIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToList();

        if (!ids.Remove(eventId))
            return;

        record.EventsIds = string.Join(',', ids);

        await eventsDb.UpdateUserEventsAsync(record.UserId, record.EventsIds);
    }

    public static async Task AddUserSessionEvent(long chatId, EventsDatabase eventsDb, string eventId)
    {
        var record = await eventsDb.GetUserAsync(chatId);

        if (record is null)
        {
            await eventsDb.AddUserAsync(chatId, eventId);
            return;
        }

        var ids = record.EventsIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToList();

        if (!ids.Contains(eventId))
            ids.Add(eventId);

        record.EventsIds = string.Join(',', ids);
        await eventsDb.UpdateUserEventsAsync(record.UserId, record.EventsIds);
    }

    public static List<int> ParseEventIds(string eventIdsString)
    {
        return eventIdsString
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x.Trim(), out var id) ? id : (int?)null)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToList();
    }

    public static string GetSubscribedEventsUserFormattedMessage(string eventsIds)
    {
        if (string.IsNullOrWhiteSpace(eventsIds))
            return "Нет подписок.";

        var events = eventsIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x, out var id) ? id : (int?)null)
            .Where(id => id != null)
            .Select(id => Events.FirstOrDefault(e => e.Id == id))
            .Where(e => e != null)
            .ToList();

        var sb = new StringBuilder();

        foreach (var ev in events)
            sb.AppendLine(ev.Name ?? "N/A");

        return sb.ToString();
    }

    private static List<int> GetUserEventsIds(string eventsIds)
    {
        var ids = eventsIds
.Split(',', StringSplitOptions.RemoveEmptyEntries)
.Select(x => int.TryParse(x.Trim(), out var id) ? id : (int?)null)
.Where(id => id.HasValue)
.Select(id => id.Value)
.ToList();

        return ids;
    }
}
