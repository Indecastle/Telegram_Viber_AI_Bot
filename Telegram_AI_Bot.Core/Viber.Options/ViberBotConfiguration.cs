namespace Telegram_AI_Bot.Core.Viber.Options;

public class ViberBotConfiguration
{
    public static readonly string Configuration = "ViberBot";

    public string Webhook { get; set; } = "";
    public string AdminPhoneNumber { get; set; } = "";
}