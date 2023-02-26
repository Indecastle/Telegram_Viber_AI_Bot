using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram_AI_Bot.Core.Events;
using Telegram_AI_Bot.Infrastructure.Services.Events.Cap.Subscriptions.Registration;
using Webinex.Flippo;

namespace Telegram_AI_Bot.Infrastructure.Services.Events;

public class ViberPostEndpointSubscription: ISubscription<ViberPostEndpointEvent>
{
    private readonly ILogger<ViberPostEndpointSubscription> _logger;

    public ViberPostEndpointSubscription(
        ILogger<ViberPostEndpointSubscription> logger)
    {
        _logger = logger;
    }
    
    public async Task HandleAsync(ViberPostEndpointEvent payload)
    {
        try
        {
            _logger.LogInformation($"Sender {payload.SenderName}({payload.SenderId}) with message: {payload.message}");
        }
        catch (Exception e)
        {
            _logger.LogError($"Unable to process items. {JsonConvert.SerializeObject(payload)}", e);
        }
    }
}