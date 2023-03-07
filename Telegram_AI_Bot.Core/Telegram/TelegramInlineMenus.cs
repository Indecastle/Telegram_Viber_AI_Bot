using System.Text;
using Askmethat.Aspnet.JsonLocalizer.Localizer;
using Microsoft.Extensions.Localization;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram_AI_Bot.Core.Models;
using Telegram_AI_Bot.Core.Models.Users;
using Telegram_AI_Bot.Core.Services.Telegram.UpdateEvent;

namespace Telegram_AI_Bot.Core.Telegram;

public static class TelegramInlineMenus
{
    public static InlineKeyboardMarkup MainMenu(IJsonStringLocalizer localizer)
    {
        InlineKeyboardMarkup inlineKeyboard = new(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(localizer.GetString("BalanceTitle"),
                        TelegramCommands.Keyboard.Balance),
                    InlineKeyboardButton.WithCallbackData(localizer.GetString("Settings"),
                        TelegramCommands.Keyboard.Settings),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(localizer.GetString("Help"), TelegramCommands.Keyboard.Help),
                },
            });
        return inlineKeyboard;
    }

    public static InlineKeyboardButton[] BackPrev(string text, string callbackData) =>
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text, callbackData),
        };

    public static InlineKeyboardMarkup HelpMenu(IJsonStringLocalizer localizer) =>
        new(
            new[]
            {
                BackPrev(localizer.GetString("BackToMainMenu"), TelegramCommands.Keyboard.MainMenu),
            });

    public static InlineKeyboardMarkup BalanceMenu(IJsonStringLocalizer localizer) =>
        new(
            new[]
            {
                BackPrev(localizer.GetString("BackToMainMenu"), TelegramCommands.Keyboard.MainMenu),
            });

    public static InlineKeyboardMarkup SettingsMenu(IJsonStringLocalizer localizer) =>
        new(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(localizer.GetString("ChangeLanguage"),
                        TelegramCommands.Keyboard.Settings_SetLanguage),
                    InlineKeyboardButton.WithCallbackData(localizer.GetString("SwitchMode"),
                        TelegramCommands.WithArgs(TelegramCommands.Keyboard.Settings, "SwitchMode")),
                    InlineKeyboardButton.WithCallbackData(localizer.GetString("DeleteContext"),
                        TelegramCommands.WithArgs(TelegramCommands.Keyboard.Settings, "DeleteContext")),
                },
                BackPrev(localizer.GetString("BackToMainMenu"), TelegramCommands.Keyboard.MainMenu),
            });

    public static string GetSettingsText(IJsonStringLocalizer l, TelegramUser user)
    {
        var str = new StringBuilder();
        
        str.AppendLine(l.GetString("SettingsText.Title"));
        str.AppendLine(l.GetString("SettingsText.Language") + user.Language);
        str.AppendLine(l.GetString("SettingsText.Mode") + l.GetString("SelectedMode_" + user.SelectedMode.Value));
        // str.Append(l.GetString("SettingsText.Mode") + "gpt-3.5-turbo");

        if (user.SelectedMode == SelectedMode.Chat)
        {
            str.AppendLine(l.GetString("SettingsText.ChatModel") + "chat-3.5-turbo");
            str.AppendLine(l.GetString("SettingsText.MaxContextTokens") + "1000");
        }
        else
        {
            str.AppendLine(l.GetString("SettingsText.ImageModel") + "dalle");
        }

        return str.ToString();
    }

    public static InlineKeyboardMarkup LanguageMenu(IJsonStringLocalizer localizer) =>
        new(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(localizer.GetString("Language.Russian"),
                        TelegramCommands.WithArgs(TelegramCommands.Keyboard.Settings_SetLanguage, "ru-RU")),
                    InlineKeyboardButton.WithCallbackData(localizer.GetString("Language.English"),
                        TelegramCommands.WithArgs(TelegramCommands.Keyboard.Settings_SetLanguage, "en-US")),
                },
                BackPrev(localizer.GetString("Back"), TelegramCommands.Keyboard.Settings),
            });
}