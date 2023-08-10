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
        // await TestEF();
        
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

                _logger.LogWarning($"Start listening for @{me.Username}");
                
                if (_currentEnvironment.IsProduction())
                    while (true)
                        await Task.Delay(2000, cts.Token);
                else
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
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    // private async Task TestEF()
    // {
    //     Console.WriteLine("------------------------");
    //     Test1();
    //     await Task.Delay(2000);
    //     Test2();
    //     await Task.Delay(100);
    //     Test3();
    // }
    //
    // private async Task Test1()
    // {
    //     
    //     using var scope = _serviceProvider.CreateScope();
    //     AppDbContext _db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    //     using var scopeTr = new TransactionScope(
    //         TransactionScopeOption.RequiresNew,
    //         new TransactionOptions { IsolationLevel = IsolationLevel.RepeatableRead },
    //         TransactionScopeAsyncFlowOption.Enabled);
    //     var user = await _db.Users.FirstAsync(x => x.UserId == 424269317);
    //     // await using var trans = await _db.Database.BeginTransactionAsync();
    //     
    //
    //     // user.IncreaseBalance(-500);
    //     user.SetSystemMessage("hello1-" + Guid.NewGuid());
    //     await Task.Delay(5000);
    //     await _db.SaveChangesAsync();
    //     // await trans.CommitAsync();
    //     scopeTr.Complete();
    //     Console.WriteLine("1111111111111111111111111111111");
    // }
    //
    // private async Task Test2()
    // {
    //     using var scope = _serviceProvider.CreateScope();
    //     AppDbContext _db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    //     var user = await _db.Users.FirstAsync(x => x.UserId == 424269317);
    //     // user.IncreaseBalance(300);
    //     user.SetSystemMessage("hello2-" + Guid.NewGuid());
    //     await _db.SaveChangesAsync();
    //     
    //     Console.WriteLine("22222222222222222222222222222222");
    // }
    //
    // private async Task Test3()
    // {
    //     using var scope = _serviceProvider.CreateScope();
    //     AppDbContext _db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    //     var user = await _db.Users.FirstAsync(x => x.UserId == 5994965427);
    //     user.IncreaseBalance(300);
    //     user.SetSystemMessage("hello3");
    //     await _db.SaveChangesAsync();
    //     Console.WriteLine("33333333333333333333333333333333");
    // }

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