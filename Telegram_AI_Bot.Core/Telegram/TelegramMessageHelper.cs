using System.Globalization;

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
    
    public static string SetDefaultCulture(string senderLanguage)
    {
        var targetLang = senderLanguage.ToLowerInvariant() switch
        {
            "ru" => "ru-RU",
            "by" => "ru-RU",
            "ua" => "ru-RU",
            _ => "en-US",   
        };

        SetCulture(targetLang);
        return targetLang;
    }
}