namespace Telegram_AI_Bot.Core.Services.Telegram.UpdateEvent;

public class TelegramCommands
{
    public class Keyboard
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
    
    public const string MainMenu = "/menu";
    public const string Settings = "/settings";
    public const string Help = "/help";
    public const string Start = "/start";
    public const string Balance = "/balance";

    public static readonly string[] All =
    {
        MainMenu, Settings, Help, Start, Balance
    };

    public static string WithArgs(string command, params string[] args) => command + " " + string.Join(' ', args);
}