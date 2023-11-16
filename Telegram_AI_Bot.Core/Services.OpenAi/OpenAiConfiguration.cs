namespace Telegram_AI_Bot.Core.Services.OpenAi;

public class OpenAiConfiguration
{
    public static readonly string Configuration = "OpenAi";

    public string? Token { get; set; } = null;
    public string? OrganizationId { get; set; } = null;
    public string? ImgurToken { get; set; } = null;
    public int? FactorText { get; set; } = null;
    public int? FactorTextGpt4 { get; set; } = null;
    public int? FactorImage { get; set; } = null;
    public decimal? TextPrice { get; set; } = null;
    public int? ImageSmallTokens { get; set; } = null;
    public int? ImageMediumTokens { get; set; } = null;
    public int? ImageLargeTokens { get; set; } = null;

    public long CalculateTokens(long cents) // 1 (dollar) * 100
    {
        var k = 1 / TextPrice * 1000 / FactorText;
        long amount = cents / 100 * (long)k!;
        return amount;
    }
}