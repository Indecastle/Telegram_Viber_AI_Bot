using Telegram_AI_Bot.Core.Services.Viber.TextReceivedService;
using Viber.Bot.NetCore.Models;
using InternalViberUser = Viber.Bot.NetCore.Models.ViberUser.User;

namespace Telegram_AI_Bot.Core.Viber;

public static class ViberMessageHelper
{
    public const string DEFAULT_BUTTON_COLOR = "#666699";
    
    public static readonly ViberKeyboardButton BackToMainMenuButton = new()
    {
        Columns = 6,
        Rows = 1,
        BackgroundColor = DEFAULT_BUTTON_COLOR,
        ActionType = "reply",
        ActionBody = KeyboardCommands.MainMenu,
        Text = "Вернуться в главное меню",
        TextVerticalAlign = "middle",
        TextHorizontalAlign = "center",
        TextOpacity = 60,
        TextSize = "regular"
    };

    public static ViberMessage.TextMessage GetSimpleTextMessage(InternalViberUser sender, string text) =>
        new()
        {
            Receiver = sender.Id,
            Sender = new InternalViberUser()
            {
                Name = "Our bot",
                Avatar = "https://i.imgur.com/K9SDD1X.png"
            },
            Text = text,
        };

    public static ViberMessage.KeyboardMessage GetKeyboardMainMenuMessage(
        InternalViberUser sender,
        string text = "Главное меню") =>
        GetDefaultKeyboardMessage(sender, text, GetDefaultKeyboard(new[]
        {
            new ViberKeyboardButton()
            {
                Columns = 2,
                Rows = 1,
                BackgroundColor = DEFAULT_BUTTON_COLOR,
                ActionType = "reply",
                ActionBody = KeyboardCommands.Balance,
                // Image = "https://i.imgur.com/BIZyTI4.png",
                Text = "Balance",
                TextVerticalAlign = "middle",
                TextHorizontalAlign = "center",
                TextOpacity = 60,
                TextSize = "regular"
            },
            new ViberKeyboardButton()
            {
                Columns = 2,
                Rows = 1,
                BackgroundColor = DEFAULT_BUTTON_COLOR,
                ActionType = "reply",
                ActionBody = KeyboardCommands.Settings,
                // Image = "https://i.imgur.com/BIZyTI4.png",
                Text = "Settings",
                TextVerticalAlign = "middle",
                TextHorizontalAlign = "center",
                TextOpacity = 60,
                TextSize = "regular"
            },
            new ViberKeyboardButton()
            {
                Columns = 2,
                Rows = 1,
                BackgroundColor = DEFAULT_BUTTON_COLOR,
                ActionType = "reply",
                ActionBody = KeyboardCommands.Help,
                // Image = "https://i.imgur.com/BIZyTI4.png",
                Text = "Help",
                TextVerticalAlign = "middle",
                TextHorizontalAlign = "center",
                TextOpacity = 60,
                TextSize = "regular"
            },
        }));

    public static ViberMessage.KeyboardMessage GetDefaultKeyboardMessage(InternalViberUser sender, string text,
        ViberKeyboard keyboard) =>
        new()
        {
            Receiver = sender.Id,
            Sender = new InternalViberUser()
            {
                Name = "Our bot",
                Avatar = "https://i.imgur.com/K9SDD1X.png"
            },
            Text = text,
            Keyboard = keyboard
        };

    public static ViberKeyboard GetDefaultKeyboard(ViberKeyboardButton[] buttons) => new()
    {
        DefaultHeight = false,
        // BackgroundColor = "#000066",
        Buttons = buttons,
    };
}