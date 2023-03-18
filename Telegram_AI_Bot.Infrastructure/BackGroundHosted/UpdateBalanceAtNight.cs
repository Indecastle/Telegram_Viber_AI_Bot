using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoreLinq;
using Telegram_AI_Bot.Core;
using Telegram_AI_Bot.Core.Common;
using Telegram_AI_Bot.Core.Models.Users;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Ports.DataAccess.Viber;
using Telegram_AI_Bot.Core.Services.OpenAi;

namespace Telegram_AI_Bot.Infrastructure.BackGroundHosted;

internal class UpdateBalanceAtNight : BackgroundService
{
    // https://crontab.cronhub.io
    private readonly string CronExpression = "0 0 0 * * *";
    private readonly ILogger<UpdateBalanceAtNight> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly CommonConfiguration _options;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateBalanceAtNight(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<CommonConfiguration> options,
        ILogger<UpdateBalanceAtNight> logger,
        IDateTimeProvider dateTimeProvider)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _options = options.Value;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using CronosPeriodicTimer timer = new CronosPeriodicTimer(CronExpression, CronFormat.IncludeSeconds, _dateTimeProvider);
        while (
            !stoppingToken.IsCancellationRequested &&
            await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                await AddGiftBalance(scope.ServiceProvider);
                
                _logger.LogInformation("Updated balance");
                await unitOfWork.CommitAsync();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }

    private async Task AddGiftBalance(IServiceProvider serviceProvide)
    {
        var users = Array.Empty<IOpenAiUser>();
        if (_options.SocialBotType == SocialBots.Telegram)
            users = await serviceProvide.GetRequiredService<IUserRepository>().GetAllWithLowBalance();
        else if (_options.SocialBotType == SocialBots.Viber)
            users = await serviceProvide.GetRequiredService<IViberUserRepository>().GetAllWithLowBalance();

        foreach (var x in users) 
            x.SetBalance(Constants.LOW_LIMIT_BALANCE);
    }
}