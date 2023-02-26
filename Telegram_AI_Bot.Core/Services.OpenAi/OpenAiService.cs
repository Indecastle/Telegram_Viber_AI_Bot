using Microsoft.Extensions.Options;
using OpenAI_API;
using OpenAI_API.Completions;
using OpenAI_API.Models;
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
        var request = new CompletionRequest(
            requestText,
            model: Model.DavinciText,
            temperature: 0.9,
            frequencyPenalty: 0,
            presencePenalty: 0.6,
            top_p: 1,
            max_tokens: 1000
        );
        request.BestOf = 1;

        var result = await _api.Completions.CreateCompletionAsync(request);
        // var tokens = result.Completions.Select(x => x.Logprobs?.TokenLogprobs).ToArray();
        // var result = await _api.Completions.CreateCompletionAsync(
        //     requestText,
        //     model: Model.DavinciText, temperature: 0.9, max_tokens: 1000);

        return result.ToString();
    }
}