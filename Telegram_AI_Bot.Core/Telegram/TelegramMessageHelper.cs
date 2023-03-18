using System.Globalization;
using MoreLinq;
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
    
    public static string FormatTextToEqualBlockWidth(string str)
    {
        // Special zero-width connector in hex format that doesn't cut off the bot:

        const string nullSeparator = "&#x200D;";

        // The maximum number of characters, upon reaching the number of which the bot starts to stretch the width of the block with buttons:

        const int maxNumberOfSymbol = 29;

        // Pad the right side of each new line with spaces and a special character, thanks to which the bot does not cut off these spaces, and then add them to the array:

        List<string> resultStringArray = new List<string>();

        while (str.Length > 0)
        {
            // Get a substring with the length of the maximum possible width of the option block:

            string partOfString = str.Substring(0, Math.Min(str.Length, maxNumberOfSymbol)).Trim();

            // Find the first space on the left of the substring to pad with spaces and a line break character:

            int positionOfCarriageTransfer = str.Length < maxNumberOfSymbol ? str.Length : partOfString.LastIndexOf(' ');
            positionOfCarriageTransfer = positionOfCarriageTransfer == -1 ? partOfString.Length : positionOfCarriageTransfer;

            // Pad the substring with spaces and a line break character at the end:

            partOfString = partOfString.Substring(0, positionOfCarriageTransfer);

            partOfString = partOfString + new string(' ', maxNumberOfSymbol - partOfString.Length) + nullSeparator;

            // Add to array of strings:

            resultStringArray.Add($"<pre>{partOfString}</pre>");

            // Leave only the unprocessed part of the string:

            str = str.Substring(positionOfCarriageTransfer).Trim();
        }

        // Send a formatted string as a column equal to the maximum width of the message that the bot does not deform:

        return string.Join("\n", resultStringArray);
    }
}