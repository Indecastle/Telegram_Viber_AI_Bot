using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram_AI_Bot.Api.Services;

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

    internal PollingServiceBase(
        IServiceProvider serviceProvider,
        ITelegramBotClient botClient,
        ILogger<PollingServiceBase<TReceiverService>> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _botClient = botClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting polling service");

        await DoWork(stoppingToken);
    }

    private async Task DoWork(CancellationToken stoppingToken)
    {
        // Make sure we receive updates until Cancellation Requested,
        // no matter what errors our ReceiveAsync get
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Create new IServiceScope on each iteration.
                // This way we can leverage benefits of Scoped TReceiverService
                // and typed HttpClient - we'll grab "fresh" instance each time

                using var cts = new CancellationTokenSource();
                var receiverOptions = new ReceiverOptions()
                {
                    AllowedUpdates = Array.Empty<UpdateType>(),
                    ThrowPendingUpdates = false,
                };
                
                var me = await _botClient.GetMeAsync(stoppingToken);
                _logger.LogInformation("Start receiving updates for {BotName}", me.Username ?? "My Awesome Bot");
                
                _botClient.StartReceiving(HandleUpdateAsync, PollingErrorHandler, receiverOptions, cts.Token);

                Console.WriteLine($"Start listening for @{me.Username}");
                Console.ReadLine();

                cts.Cancel();
            }
            // Update Handler only captures exception inside update polling loop
            // We'll catch all other exceptions here
            // see: https://github.com/TelegramBots/Telegram.Bot/issues/1106
            catch (Exception ex)
            {
                _logger.LogError("Polling failed with exception: {Exception}", ex);

                // Cooldown if something goes wrong
                await Task.Delay(TimeSpan.FromSeconds(0.5), stoppingToken);
            }
        }
    }

    async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            IUpdateHandler updateHandler = scope.ServiceProvider.GetRequiredService<UpdateHandler>();
            await updateHandler.HandleUpdateAsync(bot, update, ct);
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            Console.WriteLine($"Exception while handling {update.Type}: {ex}");
        }
#pragma warning restore CA1031
    }

    Task PollingErrorHandler(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        Console.WriteLine($"Exception while polling for updates: {ex}");
        return Task.CompletedTask;
    }
}