using Microsoft.AspNetCore.Mvc;

namespace Viber_AI_Bot.Api.Controllers;

[Route("")]
[ApiController]
public class MainController : ControllerBase
{
    
    public MainController()
    {
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