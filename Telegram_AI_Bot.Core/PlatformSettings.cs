using System.Globalization;

namespace Telegram_AI_Bot.Core;

public static class PlatformSettings
{
    public static readonly TimeSpan TIME_OFFSET = new TimeSpan(-6, 0, 0); // CST

    public static DateTimeOffset GetLocalDate(DateTimeOffset date)
    {
        return new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, TIME_OFFSET);
    }
}