namespace Telegram_AI_Bot.Core.Services.Telegram.Payments;

public class PaymentsConfiguration
{
    public static readonly string Configuration = "PaymentProviders";

    public int[]? Choices { get; set; } = null;
    public string? StripeProviderToken { get; set; } = null;
    public string? TonProviderToken { get; set; } = null;
    public (decimal Money, long Token)[] TonPriceTuples => CryptoPrices.Select(x => (x[0], (long)x[1])).ToArray();
    public decimal[][] CryptoPrices { get; set; } = null;
    public string? CryptoApiUrl { get; set; } = null;
}