using Microsoft.Extensions.DependencyInjection;

namespace Telegram_AI_Bot.Core.Services.Common;

internal static class JobServiceCollectionExtensions
{
    public static IServiceCollection AddJob<TEvent, TJob>(this IServiceCollection services, string eventName)
        where TJob : IJob<TEvent>
    {
        services.AddSingleton(new JobRegistration(eventName, typeof(TEvent), typeof(TJob)));
        return services;
    }
}