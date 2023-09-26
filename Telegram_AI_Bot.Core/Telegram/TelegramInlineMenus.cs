using System.Text;
using Askmethat.Aspnet.JsonLocalizer.Localizer;
using CryptoPay.Types;
using Microsoft.Extensions.Localization;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram_AI_Bot.Core.Common;
using Telegram_AI_Bot.Core.Models;
using Telegram_AI_Bot.Core.Models.Users;
using Telegram_AI_Bot.Core.Services.Telegram.Payments;
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

    public static InlineKeyboardMarkup BackPrevMenu(IJsonStringLocalizer localizer,  string callbackData) =>
        new(
            new[]
            {
                BackPrev(localizer.GetString("Back"), callbackData),
            });
    
    public static InlineKeyboardButton[] BackPrev(string text, string callbackData, bool isVisible = true) =>
        isVisible ? new[]
            {
                InlineKeyboardButton.WithCallbackData(text, callbackData),
            }
            : Array.Empty<InlineKeyboardButton>();

    public static InlineKeyboardMarkup HelpMenu(IJsonStringLocalizer localizer) =>
        new(
            new[]
            {
                BackPrev(localizer.GetString("BackToMainMenu"), TelegramCommands.Keyboard.MainMenu),
            });

    public static InlineKeyboardMarkup BalanceMenu(IJsonStringLocalizer localizer, bool showBackButton = true) =>
        new(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(localizer.GetString("TonCoin.BuyTokens"),
                        TelegramCommands.Keyboard.Payments),
                },
                BackPrev(localizer.GetString("BackToMainMenu"), TelegramCommands.Keyboard.MainMenu, showBackButton),
            });

    public static InlineKeyboardMarkup SettingsMenu(IJsonStringLocalizer localizer, TelegramUser user) =>
        new(TelegramMessageHelper.RemoveNullObjects(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(localizer.GetString("ChangeLanguage"),
                        TelegramCommands.Keyboard.Settings_SetLanguage),
                    InlineKeyboardButton.WithCallbackData(localizer.GetString("SwitchMode"),
                        TelegramCommands.WithArgs(TelegramCommands.Keyboard.Settings, "SwitchMode")),
                },
                new[]
                {
                    user.SelectedMode == SelectedMode.Chat
                        ? InlineKeyboardButton.WithCallbackData(localizer.GetString("ChangeChatModel"),
                            TelegramCommands.Keyboard.Settings_SetChatModel)
                        : null,
                },
                new[]
                {
                    user.SelectedMode == SelectedMode.Chat
                    ? InlineKeyboardButton.WithCallbackData(localizer.GetString("SystemMessageMenu.Button"),
                        TelegramCommands.Keyboard.Settings_SystemMessage)
                    : null
                },
                new[]
                {
                    user.SelectedMode == SelectedMode.Chat
                        ? InlineKeyboardButton.WithCallbackData(
                            localizer.GetString(user.EnabledContext ? "DisableContext" : "EnableContext"),
                            TelegramCommands.WithArgs(TelegramCommands.Keyboard.Settings, "SwitchContext"))
                        : null,
                    user.SelectedMode == SelectedMode.Chat && user.EnabledContext && user.Messages.Any()
                        ? InlineKeyboardButton.WithCallbackData(localizer.GetString("ClearContext"),
                            TelegramCommands.WithArgs(TelegramCommands.Keyboard.Settings, "ClearContext"))
                        : null,
                },
                new[]
                {
                    user.SelectedMode == SelectedMode.Chat && user.ChatModel != ChatModel.Gpt4
                        ? InlineKeyboardButton.WithCallbackData(
                            localizer.GetString(user.EnabledStreamingChat ? "DisableStreamingChat" : "EnableStreamingChat"),
                            TelegramCommands.WithArgs(TelegramCommands.Keyboard.Settings, "SwitchStreamingChat"))
                        : null,
                },
                BackPrev(localizer.GetString("BackToMainMenu"), TelegramCommands.Keyboard.MainMenu),
            }));

    public static string GetSettingsText(IJsonStringLocalizer l, TelegramUser user)
    {
        var str = new StringBuilder();

        str.AppendLine(l.GetString("SettingsText.Title"));
        str.AppendLine(l.GetString("SettingsText.Language") + user.Language);
        str.AppendLine(l.GetString("SettingsText.Mode") + l.GetString("SelectedMode_" + user.SelectedMode.Value));

        if (user.SelectedMode == SelectedMode.Chat)
        {
            str.AppendLine(l.GetString("SettingsText.ChatModel") + user.ChatModel);
            // str.AppendLine(l.GetString("SettingsText.MaxContextTokens") + "1000");
            str.AppendLine(l.GetStringYesNo("SettingsText.EnabledContext", user.EnabledContext));
            if (user.EnabledContext)
            {
                str.AppendLine(l.GetString("SettingsText.Context",
                    user.Messages.Count / 2, Constants.MAX_STORED_MESSAGES / 2));
            }
            str.AppendLine(l.GetStringYesNo("SettingsText.EnabledStreamingChat", user.EnabledStreamingChat));
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

    private static string GetLocalizedChatModelItem(IJsonStringLocalizer localizer, ChatModel model, TelegramUser user)
    {
        return localizer.GetStringYesNo($"ChatModels.{model.Value}", user.ChatModel == model, noIsVisible: false);
    }

    public static InlineKeyboardMarkup SetChatModelBegin(IJsonStringLocalizer localizer, TelegramUser user) =>
        new(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(GetLocalizedChatModelItem(localizer, ChatModel.Gpt35, user),
                        TelegramCommands.WithArgs(TelegramCommands.Keyboard.Settings_SetChatModel,
                            ChatModel.Gpt35.Value, "Begin")),
                    InlineKeyboardButton.WithCallbackData(GetLocalizedChatModelItem(localizer, ChatModel.Gpt4, user),
                        TelegramCommands.WithArgs(TelegramCommands.Keyboard.Settings_SetChatModel,
                            ChatModel.Gpt4.Value, "Begin")),
                },
            });
    
    public static InlineKeyboardMarkup SetChatModel(IJsonStringLocalizer localizer, TelegramUser user) =>
        new(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(GetLocalizedChatModelItem(localizer, ChatModel.Gpt35, user),
                        TelegramCommands.WithArgs(TelegramCommands.Keyboard.Settings_SetChatModel,
                            ChatModel.Gpt35.Value)),
                    InlineKeyboardButton.WithCallbackData(GetLocalizedChatModelItem(localizer, ChatModel.Gpt4, user),
                        TelegramCommands.WithArgs(TelegramCommands.Keyboard.Settings_SetChatModel,
                            ChatModel.Gpt4.Value)),
                },
                BackPrev(localizer.GetString("Back"), TelegramCommands.Keyboard.Settings),
            });

    public static InlineKeyboardMarkup Payments(IJsonStringLocalizer localizer, TelegramUser user, PaymentsConfiguration paymentsOptions, IExchangeRates rates, long index)
    {
        var amount = paymentsOptions.TonPriceTuples[index].Money;
        var ton = rates.GetPrice(Assets.USD, Assets.TON, amount);
        var usdt = rates.GetPrice(Assets.USD, Assets.USDT, amount);
        var usdc = rates.GetPrice(Assets.USD, Assets.USDC, amount);
        var trx = rates.GetPrice(Assets.USD, Assets.TRX, amount);
        var ltc = rates.GetPrice(Assets.USD, Assets.LTC, amount);
        var eth = rates.GetPrice(Assets.USD, Assets.ETH, amount);
        var bnb = rates.GetPrice(Assets.USD, Assets.BNB, amount);
        var btc = rates.GetPrice(Assets.USD, Assets.BTC, amount);
        
        return new(
            new[]
            {
                // new[]
                // {
                //     InlineKeyboardButton.WithCallbackData("Stripe",
                //         TelegramCommands.WithArgs(TelegramCommands.Keyboard.Payments, "Stripe")),
                // },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData($"TON ({rates.Round(ton.Value, 2)})",
                        TelegramCommands.WithArgs(TelegramCommands.Keyboard.Payments, Assets.USD.ToString(), Assets.TON.ToString(), index.ToString())),
                    InlineKeyboardButton.WithCallbackData($"USDT ({rates.Round(usdt.Value, 2)})",
                        TelegramCommands.WithArgs(TelegramCommands.Keyboard.Payments, Assets.USD.ToString(), Assets.USDT.ToString(), index.ToString())),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData($"USDC ({rates.Round(usdc.Value, 2)})",
                        TelegramCommands.WithArgs(TelegramCommands.Keyboard.Payments, Assets.USD.ToString(), Assets.USDC.ToString(), index.ToString())),
                    InlineKeyboardButton.WithCallbackData($"TRX ({rates.Round(trx.Value, 2)})",
                        TelegramCommands.WithArgs(TelegramCommands.Keyboard.Payments, Assets.USD.ToString(), Assets.TRX.ToString(), index.ToString())),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData($"ETH ({rates.Round(eth.Value, 5)})",
                        TelegramCommands.WithArgs(TelegramCommands.Keyboard.Payments, Assets.USD.ToString(), Assets.ETH.ToString(), index.ToString())),
                    InlineKeyboardButton.WithCallbackData($"BNB ({rates.Round(bnb.Value, 5)})",
                        TelegramCommands.WithArgs(TelegramCommands.Keyboard.Payments, Assets.USD.ToString(), Assets.BNB.ToString(), index.ToString())),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData($"BTC ({rates.Round(btc.Value, 6)})",
                        TelegramCommands.WithArgs(TelegramCommands.Keyboard.Payments, Assets.USD.ToString(), Assets.BTC.ToString(), index.ToString())),
                    InlineKeyboardButton.WithCallbackData($"LTC ({rates.Round(ltc.Value, 2)})",
                        TelegramCommands.WithArgs(TelegramCommands.Keyboard.Payments, Assets.USD.ToString(), Assets.LTC.ToString(), index.ToString())),
                },
                BackPrev(localizer.GetString("Back"), TelegramCommands.Keyboard.Payments),
            });
    }
    
    public static InlineKeyboardMarkup PaymentPressToPay(IJsonStringLocalizer localizer, string payUrl) =>
        new(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithUrl(localizer.GetString("TonCoin.PressToPayButton"), payUrl),
                },
            });

    public static InlineKeyboardMarkup PaymentChoices(IJsonStringLocalizer localizer, PaymentsConfiguration paymentsOptions)
    {
        return "Ton" switch
        {
            // "Stripe" => new(
            //     new[]
            //     {
            //         new[]
            //         {
            //             InlineKeyboardButton.WithCallbackData("$2",
            //                 TelegramCommands.WithArgs(TelegramCommands.Keyboard.Payments, provider, "200")),
            //             InlineKeyboardButton.WithCallbackData("$5",
            //                 TelegramCommands.WithArgs(TelegramCommands.Keyboard.Payments, provider, "500")),
            //             InlineKeyboardButton.WithCallbackData("$10",
            //                 TelegramCommands.WithArgs(TelegramCommands.Keyboard.Payments, provider, "1000")),
            //         },
            //         BackPrev(localizer.GetString("Back"), TelegramCommands.Keyboard.Payments),
            //     }),
            "Ton" => new(
                paymentsOptions.TonPriceTuples
                    .Select((x, i) => new[]
                    {
                        InlineKeyboardButton.WithCallbackData(localizer.GetString("TonCoin.ButtonItem", x.Money),
                            TelegramCommands.WithArgs(TelegramCommands.Keyboard.Payments, i.ToString()))
                    })
                    .Concat(new[] { BackPrev(localizer.GetString("Back"), TelegramCommands.Keyboard.Balance) })
                    .ToArray()),
        };
    }
    
    public static string GetPaymentsText(IJsonStringLocalizer l, TelegramUser user, PaymentsConfiguration paymentsOptions)
    {
        var str = new StringBuilder();

        // TODO: So far, only TON
        // if (provider != "Ton")
        //     return str.ToString();

        str.AppendLine(l.GetString("TonCoin.Title"));
        foreach (var (rub, token) in paymentsOptions.TonPriceTuples)
        {
            str.AppendLine(l.GetString("TonCoin.Item", rub, token.ToString("N0")));
        }
        
        return str.ToString();
    }
    
    public static InlineKeyboardMarkup SystemMessageMenu(IJsonStringLocalizer localizer, TelegramUser user) =>
        new(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(localizer.GetString("SystemMessageMenu.Change"),
                        TelegramCommands.WithArgs(TelegramCommands.Keyboard.Settings_SystemMessage,
                            "Change")),
                    InlineKeyboardButton.WithCallbackData(localizer.GetString("SystemMessageMenu.Reset"),
                        TelegramCommands.WithArgs(TelegramCommands.Keyboard.Settings_SystemMessage,
                            "Reset")),
                },
                BackPrev(localizer.GetString("Back"), TelegramCommands.Keyboard.Settings),
            });
}