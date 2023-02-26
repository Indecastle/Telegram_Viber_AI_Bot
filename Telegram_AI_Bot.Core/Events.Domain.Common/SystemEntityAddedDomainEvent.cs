using MediatR;
using Telegram_AI_Bot.Core.Models.Types;

namespace Telegram_AI_Bot.Core.Events.Domain.Common;

/// <summary>
/// System domain event. Might not be subscribed in business code.
/// </summary>
/// <param name="Entity">Added entity</param>
public record SystemEntityAddedDomainEvent(IEntity Entity) : INotification;