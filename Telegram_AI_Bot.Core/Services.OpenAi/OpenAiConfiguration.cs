namespace Telegram_AI_Bot.Core.Services.OpenAi;

public class OpenAiConfiguration
{
    public static readonly string Configuration = "OpenAi";

    public string? Token { get; set; } = null;
    public int? Factor { get; set; } = null;
    public decimal? TextPrice { get; set; } = null;
    public int? ImageSmallTokens { get; set; } = null;
    public int? ImageMediumTokens { get; set; } = null;
    public int? ImageLargeTokens { get; set; } = null;
}