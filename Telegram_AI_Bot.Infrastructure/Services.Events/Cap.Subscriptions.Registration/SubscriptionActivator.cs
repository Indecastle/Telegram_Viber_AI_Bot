using CorrelationId;
using CorrelationId.Abstractions;
using DotNetCore.CAP;
using DotNetCore.CAP.Messages;
using Microsoft.Extensions.Logging;

namespace Telegram_AI_Bot.Infrastructure.Services.Events.Cap.Subscriptions.Registration;

internal class SubscriptionActivator<TSubscription, TPayload>
    where TSubscription : ISubscription<TPayload>
{
    private readonly TSubscription _subscription;
    private readonly ILogger<SubscriptionActivator<TSubscription, TPayload>> _logger;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public SubscriptionActivator(
        TSubscription subscription,
        ILogger<SubscriptionActivator<TSubscription, TPayload>> logger,
        ICorrelationContextAccessor correlationContextAccessor)
    {
        _subscription = subscription;
        _logger = logger;
        _correlationContextAccessor = correlationContextAccessor;
    }

    public async Task HandleAsync(TPayload payload, [FromCap] CapHeader capHeader)
    {
        var messageId = capHeader[Headers.MessageId];
        var payloadName = payload.GetType().Name;
        var handlerName = _subscription.GetType().Name;

        SetupCorrelationId(capHeader);
        LogReceived(messageId, payloadName, handlerName);

        try
        {
            await _subscription.HandleAsync(payload);
            LogProceed(messageId, payloadName, handlerName);
        }
        catch (Exception ex)
        {
            LogFailed(ex, messageId, payloadName, handlerName);
            throw;
        }
        finally
        {
            _correlationContextAccessor.CorrelationContext = null;
        }
    }

    private void SetupCorrelationId(CapHeader capHeader)
    {
        if (capHeader.ContainsKey("CorrelationId"))
        {
            _correlationContextAccessor.CorrelationContext =
                new CorrelationContext(capHeader["CorrelationId"]!, "CorrelationId");
        }
    }

    private void LogFailed(Exception ex, string messageId, string payloadName, string handlerName)
    {
        const string message = "Failed to process message {Id} of type {Name} by {Handler}";
        _logger.LogError(ex, message, messageId, payloadName, handlerName);
    }

    private void LogProceed(string messageId, string payloadName, string handlerName)
    {
        const string message = "Processed message {Id} of type {Name} by {Handler}";
        _logger.LogInformation(message, messageId, payloadName, handlerName);
    }

    private void LogReceived(string messageId, string payloadName, string handlerName)
    {
        const string message = "Received message {Id} of type {Name} for handler {Handler}";
        _logger.LogInformation(message, messageId, payloadName, handlerName);
    }
}