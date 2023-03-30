namespace Telegram_AI_Bot.Core.Services.Telegram.Payments;

public class PaymentsConfiguration
{
    public static readonly string Configuration = "PaymentProviders";

    public int[]? Choices { get; set; } = null;
    public string? StripeProviderToken { get; set; } = null;
}