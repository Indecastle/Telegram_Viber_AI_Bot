using DotNetCore.CAP.Internal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Ports.DataAccess.Viber;
using Telegram_AI_Bot.Core.Ports.Events;
using Telegram_AI_Bot.Core.Services.Common;
using Telegram_AI_Bot.Infrastructure.DataAccess;
using Telegram_AI_Bot.Infrastructure.DataAccess.Repositories;
using Telegram_AI_Bot.Infrastructure.DataAccess.Repositories.Viber;
using Telegram_AI_Bot.Infrastructure.Events.Common;
using Telegram_AI_Bot.Infrastructure.Services.Events.Cap.Subscriptions.Registration;
using System.Linq;
using MediatR;
using MoreLinq.Extensions;
using Telegram_AI_Bot.Core;
using Telegram_AI_Bot.Infrastructure.BackGroundHosted;
using Telegram_AI_Bot.Infrastructure.Services.Events;

namespace Telegram_AI_Bot.Infrastructure;

public static class InfrastructureModule
{
    
    public static IServiceCollection AddInfrastructureModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services = services ?? throw new ArgumentNullException(nameof(services));
        configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        
        services.AddSingleton<IDateTimeProvider, DateTimeProviderAdapter>();
        services.AddHostedService<UpdateBalanceAtNight>();

        return services;
    }
    
    public static IServiceCollection AddDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddScoped<IUnitOfWork, UnitOfWorkAdapter>()
            .AddDbContext<AppDbContext>(options => options
                .UseSqlServer(configuration.GetConnectionString("Default"), sql => sql
                    .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                .ConfigureWarnings(warnings => warnings.Throw()));

        services
            .AddScoped<IUserRepository, UserRepositoryAdapter>();

        return services;
    }
    
    public static IServiceCollection AddViberDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddScoped<IUnitOfWork, UnitOfWorkAdapter>()
            .AddDbContext<AppDbContext>(options => options
                .UseSqlServer(configuration.GetConnectionString("Default"), sql => sql
                    .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                .ConfigureWarnings(warnings => warnings.Throw()));

        services
            .AddScoped<IViberUserRepository, ViberUserRepositoryAdapter>();

        return services;
    }

    public static IServiceCollection AddEvents(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment env)
    {
        services.AddSingleton<IConsumerServiceSelector, SubscriptionConsumerServiceSelector>();

        services
            .Where(x => x.ImplementationInstance is JobRegistration)
            .Select(x => x.ImplementationInstance)
            .Cast<JobRegistration>()
            .ToArray()
            .ForEach(x => services.AddJobSubscription(x));

        services
            .AddSubscription<ViberPostEndpointSubscription>("telegram.postendpoint");

        services
            .AddCap(options =>
            {
                options.UseEntityFramework<AppDbContext>(x => x.Schema = "dbo");
                options.UseRabbitMQ(o =>
                {
                    o.HostName = "localhost";
                    // o.ConnectionFactoryOptions = opt => { 
                    //     //rabbitmq client ConnectionFactory config
                    // };
                });
                
                if (env.IsDevelopment())
                    options.UseDashboard();

                options.FailedRetryCount = 10;
            })
            .Services
            .AddScoped<CapEventStore>()
            .AddScoped<IEventStore>(x => x.GetRequiredService<CapEventStore>())
            .AddScoped<IEventPublisherPort>(x => x.GetRequiredService<CapEventStore>());
        
        services.AddMediatR(typeof(InfrastructureModule).Assembly);
        
        return services;
    }
    
    public static IServiceCollection AddViberEvents(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment env)
    {
        services.AddSingleton<IConsumerServiceSelector, SubscriptionConsumerServiceSelector>();

        services
            .Where(x => x.ImplementationInstance is JobRegistration)
            .Select(x => x.ImplementationInstance)
            .Cast<JobRegistration>()
            .ToArray()
            .ForEach(x => services.AddJobSubscription(x));

        services
            .AddSubscription<ViberPostEndpointSubscription>("viber.postendpoint");

        services
            .AddCap(options =>
            {
                options.UseEntityFramework<AppDbContext>(x => x.Schema = "viber");
                options.UseRabbitMQ(o =>
                {
                    o.HostName = "localhost";
                    // o.ConnectionFactoryOptions = opt => { 
                    //     //rabbitmq client ConnectionFactory config
                    // };
                });
                
                // if (env.IsDevelopment())
                //     options.UseDashboard();

                options.FailedRetryCount = 10;
            })
            .Services
            .AddScoped<CapEventStore>()
            .AddScoped<IEventStore>(x => x.GetRequiredService<CapEventStore>())
            .AddScoped<IEventPublisherPort>(x => x.GetRequiredService<CapEventStore>());
        
        services.AddMediatR(typeof(InfrastructureModule).Assembly);
        
        return services;
    }
}