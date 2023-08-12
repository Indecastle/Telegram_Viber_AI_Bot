using System.Transactions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram_AI_Bot.Api.Services;
using Telegram_AI_Bot.Infrastructure.DataAccess;

namespace Telegram_AI_Bot.Api.Abstract;

/// <summary>
/// An abstract class to compose Polling background service and Receiver implementation classes
/// </summary>
/// <typeparam name="TReceiverService">Receiver implementation class</typeparam>
public abstract class PollingServiceBase<TReceiverService> : BackgroundService
    where TReceiverService : IReceiverService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger _logger;
    private IWebHostEnvironment _currentEnvironment{ get; set; } 

    internal PollingServiceBase(
        IServiceProvider serviceProvider,
        ITelegramBotClient botClient,
        ILogger<PollingServiceBase<TReceiverService>> logger,
        IWebHostEnvironment currentEnvironment)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _currentEnvironment = currentEnvironment;
        _botClient = botClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting polling service");

        await DoWork(stoppingToken);
    }

    private async Task DoWork(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var cts = new CancellationTokenSource();
                var receiverOptions = new ReceiverOptions()
                {
                    AllowedUpdates = Array.Empty<UpdateType>(),
                    ThrowPendingUpdates = false,
                };
                
                var me = await _botClient.GetMeAsync(stoppingToken);
                _logger.LogInformation("Start receiving updates for {BotName}", me.Username ?? "My Awesome Bot");
   
                await _botClient.ReceiveAsync(HandleUpdateAsync, PollingErrorHandler, receiverOptions, cts.Token);
                
                cts.Cancel();
            }
            catch (Exception ex)
            {
                _logger.LogError("Polling failed with exception: {Exception}", ex);
                
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                IUpdateHandler updateHandler = scope.ServiceProvider.GetRequiredService<UpdateHandler>();
                await updateHandler.HandleUpdateAsync(bot, update, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception while handling {update.Type}: {ex}");
            }

        }, ct);
    }

    Task PollingErrorHandler(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        _logger.LogError($"Exception while polling for updates: {ex}");
        return Task.CompletedTask;
    }
}