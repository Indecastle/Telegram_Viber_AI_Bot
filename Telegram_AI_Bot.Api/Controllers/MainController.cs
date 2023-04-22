using Askmethat.Aspnet.JsonLocalizer.Localizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram_AI_Bot.Core.Ports.DataAccess.Viber;
using Telegram_AI_Bot.Core.Services.Viber.TextReceivedService;
using Telegram_AI_Bot.Core.Viber;
using Viber.Bot.NetCore.Infrastructure;
using Viber.Bot.NetCore.Models;
using Viber.Bot.NetCore.RestApi;

namespace Telegram_AI_Bot.Api.Controllers;

[Route("[controller]")]
[ApiController]
public class MainController : ControllerBase
{
    private readonly ITelegramBotClient _botClient;
    private readonly IJsonStringLocalizer _localizer;

    public MainController(
        ITelegramBotClient botClient,
        IJsonStringLocalizer<MainController> localizer)
    {
        _botClient = botClient;
        _localizer = localizer;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] string abc)
    {
        Console.WriteLine("wow" + abc);
        return Ok("Nice, have a good day :)");
    }
}