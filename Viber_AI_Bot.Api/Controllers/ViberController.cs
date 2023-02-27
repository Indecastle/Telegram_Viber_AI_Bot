using System.Globalization;
using Askmethat.Aspnet.JsonLocalizer.Localizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Telegram_AI_Bot.Core.Ports.DataAccess.Viber;
using Telegram_AI_Bot.Core.Services.Viber.TextReceivedService;
using Telegram_AI_Bot.Core.Viber;
using Viber.Bot.NetCore.Infrastructure;
using Viber.Bot.NetCore.Models;
using Viber.Bot.NetCore.RestApi;

namespace Viber_AI_Bot.Api.Controllers;

[Route("[controller]")]
[ApiController]
public class ViberController : ControllerBase
{
    private readonly IViberBotApi _botClient;
    private readonly ViberBotConfiguration _viberOptions;
    private readonly IViberTextReceivedService _textReceivedService;
    private readonly IViberUserRepository _userRepository;
    private readonly IJsonStringLocalizer _localizer;

    public ViberController(
        IViberBotApi botClient,
        IOptions<ViberBotConfiguration> viberOptions,
        IViberTextReceivedService textReceivedService,
        IViberUserRepository userRepository,
        IJsonStringLocalizer<ViberController> localizer)
    {
        _botClient = botClient;
        _textReceivedService = textReceivedService;
        _userRepository = userRepository;
        _localizer = localizer;
        _viberOptions = viberOptions.Value;
    }

    // The service sets a webhook automatically, but if you want sets him manually then use this
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var response = await _botClient.SetWebHookAsync(new ViberWebHook.WebHookRequest(_viberOptions.Webhook));

        if (response.Content.Status == ViberErrorCode.Ok)
        {
            return Ok("Viber-bot is active");
        }
        else
        {
            return BadRequest(response.Content.StatusMessage);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ViberCallbackData update)
    {
        if (update.Event == "conversation_started")
        {
            ViberMessageHelper.SetDefaultCulture(update.User.Country);
            var text = _localizer.GetString("Welcome");
            var newMessage = ViberMessageHelper.GetKeyboardMainMenuMessage(_localizer, update.User, text);
            await _botClient.SendMessageV6Async(newMessage);
            return Ok();
        }

        if (update.Message == null)
            return Ok();

        try
        {
            return Ok();
        }
        finally
        {
            var user = await _userRepository.GetOrCreateIfNotExistsAsync(update.Sender ?? update.User);
            Response.OnCompleted(async () =>
            {
                ViberMessageHelper.SetCulture(user.Language);
                Task handler = update.Message.Type switch
                {
                    ViberMessageType.Video => throw new NotImplementedException(),
                    ViberMessageType.Text => _textReceivedService.Handle(update),
                    _ => Task.CompletedTask
                };

                await handler;
            });
        }
    }
}