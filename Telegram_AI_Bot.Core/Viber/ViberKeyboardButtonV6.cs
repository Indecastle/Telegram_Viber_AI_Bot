using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Viber.Bot.NetCore.Models;

public class ViberKeyboardButtonV6 : ViberKeyboardButton
{
    [JsonProperty("ImageScaleType")]
    public string? ImageScaleType { get; set; }

    [JsonProperty("TextShouldFit")]
    public bool? TextShouldFit { get; set; }

    [JsonProperty("Frame")]
    public ViberKeyboardButtonFrame? Frame { get; set; }
}