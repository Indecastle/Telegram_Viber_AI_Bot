using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram_AI_Bot.Api.Abstract;

namespace Telegram_AI_Bot.Api.Services;

public class PollingService : PollingServiceBase<ReceiverService>
{
    public PollingService(IServiceProvider serviceProvider, ITelegramBotClient botClient, ILogger<PollingService> logger, IWebHostEnvironment currentEnvironment)
        : base(serviceProvider, botClient, logger, currentEnvironment)
    {
    }
}