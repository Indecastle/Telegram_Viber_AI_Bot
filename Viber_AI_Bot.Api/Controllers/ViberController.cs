using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Quartz;
using Telegram_AI_Bot.Core.Events;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Ports.DataAccess.Viber;
using Telegram_AI_Bot.Core.Ports.Events;
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

    public ViberController(
        IViberBotApi botClient,
        IOptions<ViberBotConfiguration> viberOptions,
        IViberTextReceivedService textReceivedService,
        IViberUserRepository userRepository)
    {
        _botClient = botClient;
        _textReceivedService = textReceivedService;
        _userRepository = userRepository;
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
        var str = String.Empty;
        
        // IScheduler scheduler = await _schedulerFactory.GetScheduler();
        // // scheduler.Context.Clear();
        // // scheduler.Context.Add("update", update);
        //
        // IDictionary<string, object> dataMap = new Dictionary<string, object>()
        // {
        //     ["update"] = update,
        // };
        // await scheduler.TriggerJob(new JobKey("PostEndpointJob"), new JobDataMap(dataMap));
        

        if (update.Event == "conversation_started")
        {
            var text = "Добро пожаловать";
            var newMessage = ViberMessageHelper.GetKeyboardMainMenuMessage(update.User, text);
            await _botClient.SendMessageAsync<ViberResponse.SendMessageResponse>(newMessage);
            return Ok();
        }

        if (update.Message == null)
            return Ok();

        // DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(update.Timestamp);
        // var current = dateTimeOffset.ToUnixTimeSeconds();
        // var old = update.Timestamp / 1000;
        // if (old + 5 < current)
        //     return BadRequest();

        await _userRepository.CreateNewIfNotExistsAsync(update.Sender ?? update.User);
        
        
        try
        {
            return Ok();
        }
        finally
        {
            Response.OnCompleted(async () =>
            {
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