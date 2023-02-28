using Newtonsoft.Json;
using Viber.Bot.NetCore.Models;

namespace Telegram_AI_Bot.Core.Viber;

public class ViberPictureMessageV6 : ViberMessage.PictureMessage
{
    [JsonProperty("keyboard")]
    public ViberKeyboard Keyboard { get; set; }
}