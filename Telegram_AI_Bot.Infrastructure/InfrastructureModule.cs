using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Ports.DataAccess.Viber;
using Telegram_AI_Bot.Infrastructure.DataAccess;
using Telegram_AI_Bot.Infrastructure.DataAccess.Repositories;
using Telegram_AI_Bot.Infrastructure.DataAccess.Repositories.Viber;

namespace Telegram_AI_Bot.Infrastructure;

public static class InfrastructureModule
{
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
}