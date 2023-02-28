using System.Globalization;
using Askmethat.Aspnet.JsonLocalizer.Localizer;
using Microsoft.Extensions.Localization;
using Refit;
using Telegram_AI_Bot.Core.Services.Viber.TextReceivedService;
using Viber.Bot.NetCore.Models;
using Viber.Bot.NetCore.RestApi;
using InternalViberUser = Viber.Bot.NetCore.Models.ViberUser.User;

namespace Telegram_AI_Bot.Core.Viber;

public static class ViberMessageHelper
{
    public const string DEFAULT_BACKGROUND_BUTTON_COLOR = "#80aaff";
    public const string DEFAULT_BACKGROUND_BUTTON_COLOR2 = "#2b64d4";

    public static ViberKeyboardButton BackToPrevMenuButton(IJsonStringLocalizer localizer, string actionBody) =>
        GetDefaultKeyboardButton(6, 1, localizer.GetString("Back"), actionBody,
            backgroundColor: DEFAULT_BACKGROUND_BUTTON_COLOR2);

    public static ViberKeyboardButton BackToMainMenuButton(IJsonStringLocalizer localizer) =>
        GetDefaultKeyboardButton(6, 1, localizer.GetString("BackToMainMenu"), KeyboardCommands.MainMenu,
            backgroundColor: DEFAULT_BACKGROUND_BUTTON_COLOR2);


    public static ViberMessage.TextMessage GetSimpleTextMessage(InternalViberUser sender, string text) =>
        new()
        {
            Receiver = sender.Id,
            Sender = new InternalViberUser()
            {
                Name = "Chat bot",
            },
            Text = text,
        };

    public static ViberMessage.KeyboardMessage GetKeyboardMainMenuMessage(
        IJsonStringLocalizer localizer,
        InternalViberUser sender,
        string text) =>
        GetDefaultKeyboardMessage(sender, text, GetDefaultKeyboard(new[]
        {
            GetDefaultKeyboardButton(2, 3, localizer.GetString("BalanceTitle"), KeyboardCommands.Balance,
                textVerticalAlign: "bottom",
                textBackgroundGradientColor: "#004de6",
                image: "https://i.imgur.com/nhZamZl.png"),
            GetDefaultKeyboardButton(2, 3, localizer.GetString("Settings"), KeyboardCommands.Settings,
                textVerticalAlign: "bottom",
                textBackgroundGradientColor: "#004de6",
                image: "https://i.imgur.com/lrsFUrb.png"),
            GetDefaultKeyboardButton(2, 3, localizer.GetString("Help"), KeyboardCommands.Help,
                textVerticalAlign: "bottom",
                textBackgroundGradientColor: "#004de6",
                image: "https://i.imgur.com/XJMJ4a1.png"),
        }));

    public static ViberKeyboardButtonV6 GetDefaultKeyboardButton(
        int columns,
        int rows,
        string text,
        string actionBody,
        string actionType = "reply",
        string backgroundColor = DEFAULT_BACKGROUND_BUTTON_COLOR,
        string? image = null,
        string textVerticalAlign = "middle",
        string textHorizontalAlign = "center",
        string? textBackgroundGradientColor = null,
        int textOpacity = 60,
        string textSize = "large",
        string? textColor = "#ffffff",
        bool textShouldFit = false,
        bool defaultFrame = true,
        ViberKeyboardButtonFrame? frame = null) =>
        new()
        {
            Columns = columns,
            Rows = rows,
            Text = textColor == null ? text : $"<font color='{textColor}'>{text}</font>",
            ActionBody = actionBody,
            ActionType = actionType,
            BackgroundColor = backgroundColor,
            Image = image,
            ImageScaleType = "crop",
            TextVerticalAlign = textVerticalAlign,
            TextHorizontalAlign = textHorizontalAlign,
            TextBackgroundGradientColor = textBackgroundGradientColor,
            TextOpacity = textOpacity,
            TextSize = textSize,
            TextShouldFit = textShouldFit,
            Frame = defaultFrame
                ? frame ?? new()
                {
                    FrameBorderWidth = 1,
                    FrameBorderColor = "#00ffcc",
                    FrameCornerRadius = 5,
                }
                : frame,
        };

    public static ViberMessage.KeyboardMessage GetDefaultKeyboardMessage(InternalViberUser sender, string text,
        ViberKeyboard keyboard) =>
        new()
        {
            Receiver = sender.Id,
            Sender = new InternalViberUser()
            {
                Name = "Chat Bot",
                Avatar = "https://i.imgur.com/K9SDD1X.png"
            },
            Text = text,
            Keyboard = keyboard
        };

    public static ViberKeyboard GetDefaultKeyboard(ViberKeyboardButton[] buttons) => new()
    {
        DefaultHeight = false,
        // BackgroundColor = "#000066",
        ButtonsGroupRows = 7,
        Buttons = buttons,
    };

    public static void SetCulture(string language)
    {
        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(language);
    }

    public static void SetDefaultCulture(string senderLanguage)
    {
        var targetLang = senderLanguage switch
        {
            "RU" => "ru-RU",
            "BY" => "ru-RU",
            "UA" => "ru-RU",
            _ => "en-US",
        };

        SetCulture(targetLang);
    }

    public static async Task<ApiResponse<ViberResponse.SendMessageResponse>> SendMessageV6Async(this IViberBotApi api,
        ViberMessage.MessageBase message)
    {
        message.MinApiVersion = 6;
        
        ApiResponse<ViberResponse.SendMessageResponse> result = default!;
        for (int i = 0; i < 3; i++)
        {
            result = await api.SendMessageAsync<ViberResponse.SendMessageResponse>(message);
            if (result.IsSuccessStatusCode)
                break;
        }

        return result;
    }
}