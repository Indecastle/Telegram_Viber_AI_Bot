using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram_AI_Bot.Core.Services.Common;

namespace Telegram_AI_Bot.Infrastructure.Services.Events.Cap.Subscriptions.Registration;

public static class ServiceCollectionCapExtensions
{
    public static IServiceCollection AddSubscription<TSubscription>(this IServiceCollection services, string name)
        where TSubscription : class
    {
        var subscriptionRegistration = new SubscriptionRegistration(typeof(TSubscription), name);
        services.AddSingleton(subscriptionRegistration);
        services.AddScoped<TSubscription>();
        return services;
    }

    public static IServiceCollection AddJobSubscription(
        this IServiceCollection services,
        JobRegistration jobRegistration)
    {
        var jobInterfaceType = typeof(IJob<>).MakeGenericType(jobRegistration.EventType);
        services.TryAddScoped(jobInterfaceType, jobRegistration.JobType);

        var subscriptionType =
            typeof(JobSubscription<,>).MakeGenericType(jobRegistration.EventType, jobInterfaceType);
        var subscriptionRegistration = new SubscriptionRegistration(subscriptionType, jobRegistration.EventName);

        services.AddScoped(subscriptionType);
        services.AddSingleton(subscriptionRegistration);
        return services;
    }

    private class JobSubscription<TEvent, TJob> : ISubscription<TEvent>
        where TJob : IJob<TEvent>
    {
        private readonly TJob _job;

        public JobSubscription(TJob job)
        {
            _job = job;
        }

        public Task HandleAsync(TEvent payload)
        {
            return _job.HandleAsync(payload);
        }
    }
}