using MediatR;
using Telegram_AI_Bot.Core.Ports.Events;

namespace Telegram_AI_Bot.Core.Events.Domain.Common;

internal interface IEventPublisherDomainEventHandler
{
}

internal class EventPublisherDomainEventHandler<T> : IEventPublisherDomainEventHandler, INotificationHandler<T>
    where T : INotification
{
    private readonly string _topic;
    private readonly IEventPublisherPort _eventPublisherPort;

    public EventPublisherDomainEventHandler(string topic, IEventPublisherPort eventPublisherPort)
    {
        _topic = topic;
        _eventPublisherPort = eventPublisherPort;
    }

    public Task Handle(T notification, CancellationToken cancellationToken)
    {
        return _eventPublisherPort.EnqueueAsync(_topic, notification);
    }
}