namespace Telegram_AI_Bot.Core.Services.Viber.TextReceivedService;

public static class KeyboardCommands
{
    public const string MainMenu = "--mainmenu";
    public const string Balance = "--balance";
    public const string Settings = "--settings";
    public const string Settings_SetLanguage = "--settings_set_language";
    public const string Help = "--help";

    public static readonly string[] All =
    {
        MainMenu, Balance, Settings, Settings_SetLanguage, Help
    };
}