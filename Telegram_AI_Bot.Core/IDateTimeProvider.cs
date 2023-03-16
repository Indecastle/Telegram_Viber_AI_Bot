namespace Telegram_AI_Bot.Core;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
    DateTimeOffset LocalNow { get; }
}