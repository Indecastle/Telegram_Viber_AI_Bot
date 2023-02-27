using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using OpenAI_API;
using OpenAI_API.Completions;
using OpenAI_API.Models;
using InternalViberUser = Viber.Bot.NetCore.Models.ViberUser.User;

namespace Telegram_AI_Bot.Core.Services.OpenAi;

public interface IOpenAiService
{
    Task<string?> Handler(string requestText);
}

public class OpenAiService : IOpenAiService
{
    private readonly Regex rg = new Regex(@".*: *");
    
    // The following is a conversation with an AI assistant. The assistant is helpful, creative, clever, tells in great detail and very friendly
    private const string Template =
        @"The following is a conversation with an AI assistant. The assistant is helpful, creative, clever and very friendly.
You: {0}
AI:";

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
            string.Format(Template, requestText),
            model: Model.DavinciText,
            temperature: 0.9,
            frequencyPenalty: 0,
            presencePenalty: 0.6,
            top_p: 1,
            max_tokens: 1000,
            stopSequences: new[] { " You:", " AI:" }
        );
        request.BestOf = 1;

        var result = await _api.Completions.CreateCompletionAsync(request);
        // var tokens = result.Completions.Select(x => x.Logprobs?.TokenLogprobs).ToArray();
        // var result = await _api.Completions.CreateCompletionAsync(
        //     requestText,
        //     model: Model.DavinciText, temperature: 0.9, max_tokens: 1000);
        
        var text = result.ToString().Trim();
        // var match = rg.Match(text);
        // text = text.Substring(match.Index + match.Length);
        return text.Trim();
    }
}