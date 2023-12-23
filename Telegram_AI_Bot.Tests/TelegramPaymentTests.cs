using CryptoPay.Types;
using Telegram_AI_Bot.Core.Services.Telegram.Payments;

namespace Telegram_AI_Bot.Tests;

public class TelegramPaymentTests
{
    private IExchangeRates _exchangeRates;
    
    [SetUp]
    public void Setup()
    {
        _exchangeRates = new ExchangeRates
        {
            Rate_Btc_Usd = 25000M,
        };
    }

    [Test]
    public void GetPrice_When_5_USD_To_BTC()
    {
        var result = _exchangeRates.GetPrice(Assets.USD, Assets.BTC, 5);
        Assert.That(result, Is.EqualTo(0.0002M));
    }
    
    [Test]
    public void GetPrice_When_0_USD_To_BTC()
    {
        var result = _exchangeRates.GetPrice(Assets.USD, Assets.BTC, 0);
        Assert.That(result, Is.EqualTo(0M));
    }
    
    [Test]
    public void Round_When_Amount_Are_5_2345_With_Decimals_3()
    {
        var result = _exchangeRates.Round(5.2345M, 3);
        Assert.That(result, Is.EqualTo(5.235M));
    }
    
    [Test]
    public void Round_When_Amount_Are_5_2344_With_Decimals_3()
    {
        var result = _exchangeRates.Round(5.2344M, 3);
        Assert.That(result, Is.EqualTo(5.235M));
    }
}