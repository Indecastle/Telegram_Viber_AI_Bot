using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Viber.Bot.NetCore.Infrastructure;
using Viber.Bot.NetCore.Models;
using Viber.Bot.NetCore.RestApi;

namespace Viber_AI_Bot.Api.Controllers;

[Route("")]
[ApiController]
public class MainController : ControllerBase
{
    private readonly IViberBotApi _viberBotApi;
    private readonly ViberBotConfiguration _viberOptions;

    public MainController(IViberBotApi viberBotApi, IOptions<ViberBotConfiguration> viberOptions)
    {
        _viberBotApi = viberBotApi;
        _viberOptions = viberOptions.Value;
    }

    // The service sets a webhook automatically, but if you want sets him manually then use this
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok();
    }
    
    [HttpPost]
    public async Task<IActionResult> Post()
    {
        return Ok();
    }
}