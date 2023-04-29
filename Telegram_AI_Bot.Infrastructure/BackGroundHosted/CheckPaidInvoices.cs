using Askmethat.Aspnet.JsonLocalizer.Localizer;
using Cronos;
using CryptoPay;
using CryptoPay.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoreLinq;
using Telegram.Bot;
using Telegram_AI_Bot.Core;
using Telegram_AI_Bot.Core.Common;
using Telegram_AI_Bot.Core.Models.Users;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Ports.DataAccess.Viber;
using Telegram_AI_Bot.Core.Services.OpenAi;
using Telegram_AI_Bot.Core.Services.Telegram.Payments;
using Telegram_AI_Bot.Core.Telegram;

namespace Telegram_AI_Bot.Infrastructure.BackGroundHosted;

internal class CheckPaidInvoices : BackgroundService
{
    // https://crontab.cronhub.io
    private readonly string CronExpression = "*/5 * * * * *";
    private readonly ILogger<CheckPaidInvoices> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly CommonConfiguration _options;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly PaymentsConfiguration _paymentsOptions;
    private readonly CryptoPayClient _cryptoTonClient;

    public CheckPaidInvoices(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<CommonConfiguration> options,
        ILogger<CheckPaidInvoices> logger,
        IDateTimeProvider dateTimeProvider,
        IOptions<PaymentsConfiguration> paymentsOptions)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _options = options.Value;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _paymentsOptions = paymentsOptions.Value;
        _cryptoTonClient = new(_paymentsOptions.TonProviderToken!, apiUrl: _paymentsOptions.CryptoApiUrl);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using CronosPeriodicTimer timer =
            new CronosPeriodicTimer(CronExpression, CronFormat.IncludeSeconds, _dateTimeProvider);
        while (
            !stoppingToken.IsCancellationRequested &&
            await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                await Handler(scope.ServiceProvider, stoppingToken);
                
                await unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }
    }

    private async Task Handler(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var userProvider = serviceProvider.GetRequiredService<IUserRepository>();
        var cryptoTransactionRepository = serviceProvider.GetRequiredService<ICryptoTransactionRepository>();
        var telegramBotClient = serviceProvider.GetRequiredService<ITelegramBotClient>();
        var localizer = serviceProvider.GetRequiredService<IJsonStringLocalizer>();

        var invoices = await _cryptoTonClient.GetInvoicesAsync(status: Statuses.paid, count: 200);

        var endTime = _dateTimeProvider.UtcNow.UtcDateTime - TimeSpan.FromHours(5);
        var validItems = invoices.Items.Where(x => x.PaidAt > endTime).ToArray();
        var invoiceIds = validItems.Select(x => x.InvoiceId).ToArray();
        long[] newInvoiceIds = await cryptoTransactionRepository.NoExistsInvoiceIds(invoiceIds);

        var newInvoices = invoices.Items
            .Where(x => newInvoiceIds.Contains(x.InvoiceId))
            .ToArray();

        var newTokensByUserId = newInvoices
            .Select(x => (Ar: x.Payload.Split(','), Invoice: x))
            .Where(x => x.Item1.Length == 2)
            .Select(x => (UserId: long.Parse(x.Ar[0]), Tokens: long.Parse(x.Ar[1]), x.Invoice))
            .ToArray();

        var userByTelegramUserId = (await userProvider.GetAllByUserId(newTokensByUserId.Select(x => x.UserId).ToArray()))
            .ToDictionary(x => x.UserId);

        var cryptoTransactions = newTokensByUserId
            .Where(x => newInvoiceIds.Contains(x.Invoice.InvoiceId) && userByTelegramUserId.ContainsKey(x.UserId))
            .Select(x =>
                CryptoTransaction.Create(x.Invoice, userByTelegramUserId[x.UserId], x.Invoice.Asset, x.Tokens))
            .ToArray();

        await cryptoTransactionRepository.AddRangeAsync(cryptoTransactions);

        foreach (var invoice in cryptoTransactions)
        {
            var user = userByTelegramUserId[invoice.TelegramUserId];
            user.IncreaseBalance(invoice.Tokens);
            TelegramMessageHelper.SetCulture(user.Language);
            
            await telegramBotClient.SendTextMessageAsync(
                chatId: user.UserId,
                text: localizer.GetString("TonCoin.YouHaveBoughtTokens", invoice.Tokens),
                cancellationToken: cancellationToken);
        }
        
        if (cryptoTransactions.Any())
            _logger.LogInformation("Added new {CryptoTransactions} transactions", cryptoTransactions.Length);
    }
}