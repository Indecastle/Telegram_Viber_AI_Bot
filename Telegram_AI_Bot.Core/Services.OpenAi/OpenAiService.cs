using Microsoft.Extensions.Options;
using OpenAI_API;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Ports.DataAccess.Viber;
using Telegram_AI_Bot.Core.Services.Viber.TextReceivedService;
using Telegram_AI_Bot.Core.Viber;
using Viber.Bot.NetCore.Models;
using Viber.Bot.NetCore.RestApi;
using InternalViberUser = Viber.Bot.NetCore.Models.ViberUser.User;

namespace Telegram_AI_Bot.Core.Services.OpenAi;


public interface IOpenAiService
{
    Task<string?> Handler(string requestText);
}

public class OpenAiService : IOpenAiService
{
    private readonly OpenAiConfiguration _openAiptions;
    private readonly OpenAIAPI _api;
    
    public OpenAiService(
        IOptions<OpenAiConfiguration> openAiptions)
    {
        _openAiptions = openAiptions.Value;
        _api = new OpenAIAPI(_openAiptions.Token);
    }

    public async Task<string?> Handler(string requestText)
    {
        var result = await _api.Completions.GetCompletion(requestText);
        return result;
    }
}