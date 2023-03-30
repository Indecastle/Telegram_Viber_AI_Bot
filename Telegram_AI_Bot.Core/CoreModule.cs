using Microsoft.Extensions.DependencyInjection;
using Telegram_AI_Bot.Core.Services.OpenAi;
using Telegram_AI_Bot.Core.Services.Telegram.OpenAi;
using Telegram_AI_Bot.Core.Services.Telegram.Payments;
using Telegram_AI_Bot.Core.Services.Telegram.UpdateEvent;
using Telegram_AI_Bot.Core.Services.Viber.OpenAi;
using Telegram_AI_Bot.Core.Services.Viber.TextReceivedService;

namespace Telegram_AI_Bot.Core;

public static class CoreModule
{
    public static IServiceCollection AddTelegramCoreModule(this IServiceCollection services)
    {
        services = services ?? throw new ArgumentNullException(nameof(services));
        
        services
            .AddScoped<IBotOnMessageReceivedService, BotOnMessageReceivedService>()
            .AddScoped<ITelegramOpenAiService, TelegramOpenAiService>()
            .AddScoped<ITelegramPaymentsService, TelegramPaymentsService>()
            .AddScoped<IBotOnCallbackQueryService, BotOnCallbackQueryService>();

        services
            .AddScoped<IOpenAiService, OpenAiService>();
            
        return services;
    }
    
    public static IServiceCollection AddViberCoreModule(this IServiceCollection services)
    {
        services = services ?? throw new ArgumentNullException(nameof(services));
        
        services
            .AddScoped<IViberTextReceivedService, ViberTextReceivedService>()
            .AddScoped<IViberKeyboardService, ViberKeyboardService>()
            .AddScoped<IViberOpenAiService, ViberOpenAiService>();
        
        services
            .AddScoped<IOpenAiService, OpenAiService>();

        return services;
    }
}
