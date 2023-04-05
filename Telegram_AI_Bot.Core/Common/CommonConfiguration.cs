namespace Telegram_AI_Bot.Core.Common;

public class CommonConfiguration
{
    public static readonly string Configuration = "Common";

    public string SocialBotType { get; set; } = "";
    public string BotUrl { get; set; } = "";
}

public static class SocialBots
{
    public const string Telegram = "Telegram";
    public const string Viber = "Viber";
}