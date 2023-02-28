using Newtonsoft.Json;
using Viber.Bot.NetCore.Models;

namespace Telegram_AI_Bot.Core.Viber;

public class ViberContactMessageV6 : ViberMessage.ContactMessage
{
    public ViberContactMessageV6(ViberKeyboardMessageV6 keyboardMessage)
    {
        Receiver = keyboardMessage.Receiver;
        Sender = keyboardMessage.Sender;
        Keyboard = keyboardMessage.Keyboard;
    }
    
    [JsonProperty("keyboard")]
    public ViberKeyboard Keyboard { get; set; }
}