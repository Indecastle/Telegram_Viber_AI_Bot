namespace Telegram_AI_Bot.Core.Ports.Events;

public static class EventPublisherExtensions
{
    public static async Task<string> EnqueueAsync<T>(this IEventPublisherPort eventPublisher, string kind, T @event)
    {
        eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        @event = @event ?? throw new ArgumentNullException(nameof(@event));
        var args = new PublishEventArgs(kind, @event);
        return await eventPublisher.EnqueueAsync(args);
    }
}