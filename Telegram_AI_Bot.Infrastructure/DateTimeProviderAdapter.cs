using Telegram_AI_Bot.Core;

namespace Telegram_AI_Bot.Infrastructure;

internal class DateTimeProviderAdapter : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateTimeOffset LocalNow => UtcNow.ToOffset(PlatformSettings.TIME_OFFSET);
}