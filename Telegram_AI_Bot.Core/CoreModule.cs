using System.Runtime.CompilerServices;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram_AI_Bot.Core.Events.Domain.Common;
using Telegram_AI_Bot.Core.Ports.Events;
using Telegram_AI_Bot.Core.Services.OpenAi;
using Telegram_AI_Bot.Core.Services.Telegram.OpenAi;
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
            .AddScoped<IBotOnCallbackQueryService, BotOnCallbackQueryService>();

        services
            .AddSingleton<IOpenAiService, OpenAiService>();
            
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
    
    private static IServiceCollection AddEventPublishedDomainEventHandler<TEvent>(
        this IServiceCollection services,
        string topic)
        where TEvent : INotification
    {
        return services.AddScoped<INotificationHandler<TEvent>>(sp =>
            new EventPublisherDomainEventHandler<TEvent>(topic, sp.GetRequiredService<IEventPublisherPort>()));
    }
}
