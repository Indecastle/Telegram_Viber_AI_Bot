using CryptoPay.Types;

namespace Telegram_AI_Bot.Core.Services.Telegram.Payments;

public interface IExchangeRates
{
    decimal? Rate_Ton_Rub { get; }
    decimal? Rate_Usdt_Rub { get; }
    decimal? Rate_Usdc_Rub { get; }
    decimal? Rate_Busd_Rub { get; }
    decimal? Rate_BTC_Rub { get; }
    decimal? Rate_Eth_Rub { get; }
    decimal? Rate_Bnb_Rub { get; }

    decimal? GetPrice(Assets from, Assets to, decimal amount);
    decimal Round(decimal amount, int decimals = 4);
}

public class ExchangeRates: IExchangeRates
{
    public decimal? Rate_Ton_Rub { get; set; }
    public decimal? Rate_Usdt_Rub { get; set; }
    public decimal? Rate_Usdc_Rub { get; set; }
    public decimal? Rate_Busd_Rub { get; set; }
    public decimal? Rate_BTC_Rub { get; set; }
    public decimal? Rate_Eth_Rub { get; set; }
    public decimal? Rate_Bnb_Rub { get; set; }
    
    public decimal? GetPrice(Assets from, Assets to, decimal amount = 4)
    {
        return from switch
        {
            Assets.RUB => to switch
            {
                Assets.TON => amount / Rate_Ton_Rub,
                Assets.USDT => amount / Rate_Usdt_Rub,
                Assets.USDC => amount / Rate_Usdc_Rub,
                Assets.BUSD => amount / Rate_Busd_Rub,
                Assets.BTC => amount / Rate_BTC_Rub,
                Assets.ETH => amount / Rate_Eth_Rub,
                Assets.BNB => amount / Rate_Bnb_Rub,
            }
        };
    }

    public decimal Round(decimal amount, int decimals = 3)
    {
        return Math.Round(amount, decimals, MidpointRounding.ToPositiveInfinity);
    }
}

