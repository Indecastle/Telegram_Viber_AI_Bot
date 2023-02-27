using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Viber.Bot.NetCore.Models;

public class ViberKeyboardButtonFrame
{
    [JsonProperty("BorderWidth")]
    public int? FrameBorderWidth { get; set; }
    
    [JsonProperty("BorderColor")]
    public string? FrameBorderColor { get; set; }
    
    [JsonProperty("CornerRadius")]
    public int? FrameCornerRadius { get; set; }
}