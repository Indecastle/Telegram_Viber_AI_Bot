namespace Telegram_AI_Bot.Infrastructure.Services.Events.Cap.Subscriptions.Registration;

public interface ISubscription<TPayload>
{
    Task HandleAsync(TPayload payload);
}