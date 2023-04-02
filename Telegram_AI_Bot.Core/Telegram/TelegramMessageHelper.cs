using System.Globalization;
using Askmethat.Aspnet.JsonLocalizer.Localizer;
using Microsoft.Extensions.Localization;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram_AI_Bot.Core.Telegram;

public static class TelegramMessageHelper
{
    public static void SetCulture(string language)
    {
        try
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(language);
        }
        catch (CultureNotFoundException)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
        }
    }
    
    public static string SetDefaultCulture(string? senderLanguage)
    {
        var targetLang = senderLanguage?.ToLowerInvariant() switch
        {
            "ru" => "ru-RU",
            "by" => "ru-RU",
            "ua" => "ru-RU",
            _ => "en-US",   
        };

        SetCulture(targetLang);
        return targetLang;
    }

    public static IEnumerable<IEnumerable<InlineKeyboardButton>> RemoveNullObjects(
        IEnumerable<IEnumerable<InlineKeyboardButton?>> inlineKeyboard)
    {
        var x = inlineKeyboard
            .Select(x => 
                x.Where(x => x is not null).ToArray())
            .ToArray();
        
        // x.ForEach(group => group.ForEach(b => b.Text = FormatTextToEqualBlockWidth(b.Text)));
        return x;
    }

    public static string GetStringYesNo(this IJsonStringLocalizer localizer, string name, bool value, bool noIsVisible = true)
    {
        var noValue = noIsVisible ? "❌" : "";
        return localizer.GetString(name) + (value ? "✅" : noValue);
    }
}