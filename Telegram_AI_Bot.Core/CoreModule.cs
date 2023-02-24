using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Telegram_AI_Bot.Core.Services.BotReceivedMessage;
using Telegram_AI_Bot.Core.Services.OpenAi;
using Telegram_AI_Bot.Core.Services.Viber.OpenAi;
using Telegram_AI_Bot.Core.Services.Viber.TextReceivedService;

namespace Telegram_AI_Bot.Core;

public static class CoreModule
{
    public static IServiceCollection AddCoreModule(this IServiceCollection services)
    {
        services = services ?? throw new ArgumentNullException(nameof(services));
        
        services
            .AddScoped<IBotOnMessageReceivedService, BotOnMessageReceivedService>();

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
            .AddSingleton<IOpenAiService, OpenAiService>();

        return services;
    }
}
