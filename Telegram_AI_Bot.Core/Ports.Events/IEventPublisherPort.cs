namespace Telegram_AI_Bot.Core.Ports.Events;

public interface IEventPublisherPort
{
    Task<string> EnqueueAsync(PublishEventArgs args);
}