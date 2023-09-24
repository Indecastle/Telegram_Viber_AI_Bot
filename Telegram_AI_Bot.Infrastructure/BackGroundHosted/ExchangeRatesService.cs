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

public class ExchangeRatesService : BackgroundService
{
    // https://crontab.cronhub.io
    private readonly string CronExpression = "0 0 0 * * *";
    private readonly ILogger<ExchangeRatesService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly CommonConfiguration _options;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly PaymentsConfiguration _paymentsOptions;
    private readonly CryptoPayClient _cryptoTonClient;
    private readonly ExchangeRates _rates;

    public ExchangeRatesService(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<CommonConfiguration> options,
        ILogger<ExchangeRatesService> logger,
        IDateTimeProvider dateTimeProvider,
        IOptions<PaymentsConfiguration> paymentsOptions,
        IExchangeRates rates)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _options = options.Value;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _rates = (ExchangeRates)rates;
        _paymentsOptions = paymentsOptions.Value;
        _cryptoTonClient = new(_paymentsOptions.TonProviderToken!, apiUrl: _paymentsOptions.CryptoApiUrl);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await UpdateRates(stoppingToken);
        
        using CronosPeriodicTimer timer =
            new CronosPeriodicTimer(CronExpression, CronFormat.IncludeSeconds, _dateTimeProvider);
        while (
            !stoppingToken.IsCancellationRequested &&
            await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();

                await UpdateRates(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }

    private async Task UpdateRates(CancellationToken cancellationToken)
    {
        var rates = await _cryptoTonClient.GetExchangeRatesAsync(cancellationToken);
        _rates.Rate_Ton_Usd = (decimal?)rates.FirstOrDefault(x => x.Source == Assets.TON && x.Target == Assets.USD)?.Rate;
        _rates.Rate_Usdt_Usd = (decimal?)rates.FirstOrDefault(x => x.Source == Assets.USDT && x.Target == Assets.USD)?.Rate;
        _rates.Rate_Usdc_Usd = (decimal?)rates.FirstOrDefault(x => x.Source == Assets.USDC && x.Target == Assets.USD)?.Rate;
        _rates.Rate_Trx_Usd = (decimal?)rates.FirstOrDefault(x => x.Source == Assets.TRX && x.Target == Assets.USD)?.Rate;
        // _rates.Rate_Ltc_Usd = (decimal?)rates.FirstOrDefault(x => x.Source == Assets.LTC && x.Target == Assets.USD)?.Rate;
        _rates.Rate_Btc_Usd = (decimal?)rates.FirstOrDefault(x => x.Source == Assets.BTC && x.Target == Assets.USD)?.Rate;
        _rates.Rate_Eth_Usd = (decimal?)rates.FirstOrDefault(x => x.Source == Assets.ETH && x.Target == Assets.USD)?.Rate;
        _rates.Rate_Bnb_Usd = (decimal?)rates.FirstOrDefault(x => x.Source == Assets.BNB && x.Target == Assets.USD)?.Rate;
        
        _logger.LogWarning("Updated rates");
    }
}