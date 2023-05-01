using CryptoPay;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Services.Telegram.Payments;

namespace Telegram_AI_Bot.Infrastructure.BackGroundHosted;

public class StartProjectBackgroundService : BackgroundService
{
    private readonly ILogger<StartProjectBackgroundService> _logger;
    private readonly PaymentsConfiguration _paymentsOptions;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public StartProjectBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<StartProjectBackgroundService> logger,
        IOptions<PaymentsConfiguration> paymentsOptions)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _paymentsOptions = paymentsOptions.Value;
        new CryptoPayClient(_paymentsOptions.TonProviderToken!, apiUrl: _paymentsOptions.CryptoApiUrl);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var userProvider = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var users = await userProvider.GetAllTyping();

        foreach (var user in users)
            user.SetTyping(false);
        
        if (users.Any())
            _logger.LogWarning($"{users.Length} users had a field reset");

        await unitOfWork.CommitAsync();
    }
}