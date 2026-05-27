using clrhost;
using Telegram.Bot;

internal sealed class EventNotifier
{
    private readonly HashSet<string> _sent = new();
    private readonly EventsDatabase _eventsDb;
    private readonly KeyboardFactory _keyboardFactory;
    private readonly ITelegramBotClient _client;

    public EventNotifier(
        EventsDatabase eventsDb,
        KeyboardFactory keyboardFactory,
        ITelegramBotClient client)
    {
        _eventsDb = eventsDb;
        _keyboardFactory = keyboardFactory;
        _client = client;
    }

    public void StartNotifications()
    {
        _ = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    if (EventService.Events is null || EventService.Events.Count == 0)
                    {
                        await Task.Delay(Timers.EVENTS_TASK_TIME);
                        continue;
                    }

                    await ProcessNotifications();

                    if (DateTime.UtcNow.Hour == 0)
                        _sent.Clear();

                    await Task.Delay(Timers.EVENTS_TASK_TIME);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error in EventNotifier: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
        });
    }

    private async Task ProcessNotifications()
    {
        var nowUtc = DateTime.UtcNow;
        var nowMsk = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow"));

        var users = await _eventsDb.GetAllUsersAsync();

        foreach (var user in users)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(user.EventsIds))
                    continue;

                var ids = EventService.ParseEventIds(user.EventsIds);

                foreach (var eventId in ids)
                {
                    await ProcessEventNotifications(user, eventId, nowUtc);
                }
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex)
            {
                if (ex.ErrorCode == 403)
                {
                    Console.WriteLine($"User {user.UserId} blocked bot");
                    continue;
                }
            }
        }
    }

    private async Task ProcessEventNotifications(UserRecord user, int eventId, DateTime nowUtc)
    {
        var ev = EventService.Events.FirstOrDefault(e => e.Id == eventId);
        if (ev == null)
        {
            Console.WriteLine($"⚠️ Событие {eventId} не найдено");
            return;
        }

        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(ev.Timezone);
        }
        catch
        {
            Console.WriteLine($"❌ Неверный таймзон: {ev.Timezone}");
            return;
        }

        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz);
        var today = nowLocal.Date;

        foreach (var rule in ev.Rules)
        {
            if (!DateService.IsDayMatch(rule.DayType, today))
                continue;

            foreach (var time in rule.Times)
            {
                var occurrenceLocal = DateService.ExpandTimeWindow(today, time).First();
                var occurrenceUtc = TimeZoneInfo.ConvertTimeToUtc(occurrenceLocal, tz);

                foreach (var minutesBefore in new[] { 10, 5, 1 })
                {
                    await SendNotificationBefore(user, ev, occurrenceUtc, minutesBefore, nowUtc);
                }

                await SendStartNotification(user, ev, occurrenceUtc, nowUtc);
            }
        }
    }

    private async Task SendNotificationBefore(
        UserRecord user,
        Event ev,
        DateTime occurrenceUtc,
        int minutesBefore,
        DateTime nowUtc)
    {
        var notifyUtc = occurrenceUtc.AddMinutes(-minutesBefore);
        var key = $"{user.UserId}-{ev.Id}-{occurrenceUtc:yyyyMMddHHmm}-{minutesBefore}";

        if (_sent.Contains(key))
            return;

        if (DateService.IsTimeMatch(nowUtc, notifyUtc))
        {
            await _client.SendMessage(
                text: $"⏰ <b>{ev.Name}</b>\nНачнется через <b>{minutesBefore} мин!</b>",
                chatId: user.UserId,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
               
            _sent.Add(key);
        }
    }

    private async Task SendStartNotification(
      UserRecord user,
      Event ev,
      DateTime occurrenceUtc,
      DateTime nowUtc)
    {
        var startKey = $"{user.UserId}-{ev.Id}-{occurrenceUtc:yyyyMMddHHmm}-start";

        if (_sent.Contains(startKey))
            return;

        if (DateService.IsTimeMatch(nowUtc, occurrenceUtc))
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(ev.Timezone);
            var startLocal = TimeZoneInfo.ConvertTimeFromUtc(occurrenceUtc, tz);

            var message = $@"
🚨  <b>{ev.Name}</b> началось!
Приятного время провождения
";

            await _client.SendMessage(
                text: message.Trim(),
                chatId: user.UserId,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);

            _sent.Add(startKey);
        }
    }
}
