using System.Globalization;

namespace clrhost
{
    internal static class DateService
    {
        public static string GetTimeInfo(TimeWindow? timeWindow)
        {
            if (timeWindow == null) return "Не указано";
            return $"{timeWindow.Start:hh\\:mm}";
        }

        public static bool IsDayMatch(DayType type, DateTime date)
        {
            return type switch
            {
                DayType.Daily => true,
                DayType.Weekdays => date.DayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday,
                DayType.Weekends => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
                _ => false
            };
        }

        public static IEnumerable<DateTime> ExpandTimeWindow(DateTime date, TimeWindow w)
        {
            yield return date + w.Start;
        }

        public static bool IsTimeMatch(DateTime nowUtc, DateTime targetUtc)
        {
            return Math.Abs((nowUtc - targetUtc).TotalSeconds) <= 30;
        }

        public static string AddDateToQuestion(string question)
        {
            var questionTemp = question.Trim().RemoveLeadingNumber();
            var ru = new CultureInfo("ru-RU");

            if (QuestionsConstants.DayQuestionsPatterns.Any(x => questionTemp.Contains(x)))
                return $"{question}\n\n<b>Сегодня:</b> <code>{DateTime.UtcNow:dd}</code>";

            if (QuestionsConstants.MonthQuestionsPatterns.Any(x => questionTemp.Contains(x)))
                return $"{question}\n\n<b>Сегодня:</b> <code>{DateTime.UtcNow.ToString("MMMM", ru)}</code>";

            if (QuestionsConstants.YearQuestionsPatterns.Any(x => questionTemp.Contains(x)))
                return $"{question}\n\n<b>Сегодня:</b> <code>{DateTime.UtcNow:yyyy}</code>";

            return question;
        }

        public static string GetCurrentDateValueByQuestion(string question)
        {
            var questionTemp = question.Trim().RemoveLeadingNumber();
            var now = DateTime.UtcNow;
            var ru = new CultureInfo("ru-RU");

            if (QuestionsConstants.DayQuestionsPatterns.Any(p => questionTemp.Contains(p)))
                return now.ToString("dd");

            if (QuestionsConstants.MonthQuestionsPatterns.Any(p => questionTemp.Contains(p)))
                return now.ToString("MMMM", ru);

            if (QuestionsConstants.YearQuestionsPatterns.Any(p => questionTemp.Contains(p)))
                return now.ToString("yyyy");

            return null;
        }

        public static string FormatDayType(DayType type) => type switch
        {
            DayType.Daily => "Каждый день",
            DayType.Weekdays => "Будни",
            DayType.Weekends => "Выходные",
            DayType.OddDays => "Нечётные дни",
            DayType.EvenDays => "Чётные дни",
            _ => "Неизвестно"
        };
    }
}