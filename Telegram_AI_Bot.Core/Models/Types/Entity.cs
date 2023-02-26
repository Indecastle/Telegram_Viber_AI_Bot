using MediatR;

namespace Telegram_AI_Bot.Core.Models.Types;

public interface IEntity
{
    ICollection<INotification> Events { get; }

    bool HasAnyDomainEvent() => Events.Any();
    
    INotification[] PopDomainEvents()
    {
        var events = Events.ToArray();
        Events.Clear();
        return events;
    }
}