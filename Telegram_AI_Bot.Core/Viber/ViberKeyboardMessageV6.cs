using Newtonsoft.Json;
using Viber.Bot.NetCore.Models;

namespace Telegram_AI_Bot.Core.Viber;

public class ViberKeyboardMessageV6 : ViberMessage.KeyboardMessage
{
    [JsonProperty("media")]
    public string Media { get; set; }

    [JsonProperty("thumbnail")]
    public string Thumbnail { get; set; }
}