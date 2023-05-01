using System.Globalization;
using Askmethat.Aspnet.JsonLocalizer.Localizer;
using Microsoft.Extensions.Localization;
using OpenAI.Chat;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram_AI_Bot.Core.Models;
using TiktokenSharp;

namespace Telegram_AI_Bot.Core.Telegram;

public static class TelegramMessageHelper
{
    public static readonly TikToken TikTokenModel = TikToken.EncodingForModel("gpt-4");
    
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
        var noValue = noIsVisible ? " ❌" : "";
        return localizer.GetString(name) + (value ? " ✅" : noValue);
    }

    public static int GetNumTokensFromMessages(ChatModel model, IReadOnlyCollection<Message> messages)
    {
        var tokensPerMessage = model == ChatModel.Gpt35 ? 4 : 3; // every message follows <|start|>{role/name}\n{content}<|end|>\n
        
        var numTokens = messages.Aggregate(0, (num, promt) =>
            num + TikTokenModel.Encode(promt.Role.ToString()).Count + TikTokenModel.Encode(promt.Content).Count);
        
        numTokens += tokensPerMessage * messages.Count;
        numTokens += 3; // every reply is primed with <|start|>assistant<|message|>

        return numTokens;
    }
}