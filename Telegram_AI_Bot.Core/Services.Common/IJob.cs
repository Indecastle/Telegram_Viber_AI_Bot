namespace Telegram_AI_Bot.Core.Services.Common;

/// <summary>
///     IEventHandler used to handle events received from background event processing mechanizm
/// </summary>
/// <typeparam name="TEvent">Event type</typeparam>
public interface IJob<TEvent>
{
    Task HandleAsync(TEvent @event);
}