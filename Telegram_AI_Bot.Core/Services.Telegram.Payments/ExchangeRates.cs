using CryptoPay.Types;

namespace Telegram_AI_Bot.Core.Services.Telegram.Payments;

public interface IExchangeRates
{
    decimal? Rate_Ton_Usd { get; }
    decimal? Rate_Usdt_Usd { get; }
    decimal? Rate_Usdc_Usd { get; }
    decimal? Rate_Busd_Usd { get; }
    decimal? Rate_BTC_Usd { get; }
    decimal? Rate_Eth_Usd { get; }
    decimal? Rate_Bnb_Usd { get; }

    decimal? GetPrice(Assets from, Assets to, decimal amount);
    decimal Round(decimal amount, int decimals = 4);
}

public class ExchangeRates: IExchangeRates
{
    public decimal? Rate_Ton_Usd { get; set; }
    public decimal? Rate_Usdt_Usd { get; set; }
    public decimal? Rate_Usdc_Usd { get; set; }
    public decimal? Rate_Busd_Usd { get; set; }
    public decimal? Rate_BTC_Usd { get; set; }
    public decimal? Rate_Eth_Usd { get; set; }
    public decimal? Rate_Bnb_Usd { get; set; }
    
    public decimal? GetPrice(Assets from, Assets to, decimal amount = 4)
    {
        return from switch
        {
            Assets.USD => to switch
            {
                Assets.TON => amount / Rate_Ton_Usd,
                Assets.USDT => amount / Rate_Usdt_Usd,
                Assets.USDC => amount / Rate_Usdc_Usd,
                Assets.BUSD => amount / Rate_Busd_Usd,
                Assets.BTC => amount / Rate_BTC_Usd,
                Assets.ETH => amount / Rate_Eth_Usd,
                Assets.BNB => amount / Rate_Bnb_Usd,
            }
        };
    }

    public decimal Round(decimal amount, int decimals = 3)
    {
        return Math.Round(amount, decimals, MidpointRounding.ToPositiveInfinity);
    }
}

