namespace Telegram_AI_Bot.Core.Services.Telegram.UpdateEvent;

public class TelegramCommands
{
    public class Keyboard
    {
        public const string MainMenu = "--mainmenu";
        public const string Balance = "--balance";
        public const string Settings = "--settings";
        public const string Settings_SetLanguage = "--settings_Set_Language";
        public const string Settings_SetChatModel = "--settings_SetChatModel";
        public const string Settings_SystemMessage = "--settings_SystemMessage";
        public const string Help = "--help";
        public const string Payments = "--payments";

        public static readonly string[] All =
        {
            MainMenu, Balance, Settings, Settings_SetLanguage, Settings_SetChatModel, Settings_SystemMessage, Help, Payments
        };
    }
    
    public const string MainMenu = "/menu";
    public const string Settings = "/settings";
    public const string Help = "/help";
    public const string Start = "/start";
    public const string Balance = "/balance";
    public const string Payments = "/payments";

    public static readonly string[] All =
    {
        MainMenu, Settings, Help, Start, Balance
    };

    public static string WithArgs(string command, params string[] args) => command + " " + string.Join(' ', args);
}