using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenAI_API;
using OpenAI_API.Completions;
using OpenAI_API.Models;
using Telegram_AI_Bot.Core.Models.Viber.Users;
using InternalViberUser = Viber.Bot.NetCore.Models.ViberUser.User;

namespace Telegram_AI_Bot.Core.Services.OpenAi;

public interface IOpenAiService
{
    Task<string?> ChatHandler(string requestText, ViberUser user);
    Task<string?> ImageHandler(string requestText, ViberUser user);
}

public class OpenAiService : IOpenAiService
{
    // The following is a conversation with an AI assistant. The assistant is helpful, creative, clever, tells in great detail and very friendly
    private const string Template =
        "The following is a conversation with an AI assistant. The assistant is helpful, creative, clever and very friendly.\n\n";

    private readonly OpenAiConfiguration _openAiptions;
    private readonly OpenAIAPI _api;

    public OpenAiService(
        IOptions<OpenAiConfiguration> openAiptions)
    {
        _openAiptions = openAiptions.Value;
        _api = new OpenAIAPI(_openAiptions.Token);
    }

    public async Task<string?> ChatHandler(string requestText, ViberUser user)
    {
        var now = DateTimeOffset.UtcNow;
        requestText = requestText.Trim();

        var stringBuilder = new StringBuilder();

        stringBuilder.Append(Template);
        
        foreach (var message in user.Messages.OrderBy(x => x.CreatedAt))
        {
            var who = message.IsMe ? "You: " : "AI: ";
            stringBuilder.Append( $"{who}{message.Text}\n");
        }
        
        stringBuilder.Append( $"You: {requestText}\nAI: ");

        var request = new CompletionRequest(
            stringBuilder.ToString(),
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

        var text = result.ToString().Trim();
        
        user.AddMessage(requestText, true, now);
        user.AddMessage(text, false, now);
        user.RemoveUnnecessary();
        
        return text;
    }

    public async Task<string?> ImageHandler(string requestText, ViberUser user)
    {
        var images = await GetImages(new()
        {
            prompt = requestText.Trim(),
            n = 1,
            size = "512x512"
        });

        return images.FirstOrDefault();
    }
    
    private async Task<string[]> GetImages(DalleInput input)
    {
        ResponseModel resp = new();
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _openAiptions.Token);
            var Message = await client.PostAsync("https://api.openai.com/v1/images/generations",
                new StringContent(JsonConvert.SerializeObject(input),
                    Encoding.UTF8, "application/json"));
            if (Message.IsSuccessStatusCode)
            {
                var content = await Message.Content.ReadAsStringAsync();
                resp = JsonConvert.DeserializeObject<ResponseModel>(content);
            }
        }

        return resp?.data?.Select(x => x.url ?? string.Empty).ToArray() ?? Array.Empty<string>();
    }
    
    public class DalleInput
    {
        public string? prompt { get; set; }
        public short? n { get; set; }
        public string? size { get; set; }
    }
    
    private class Link
    {
        public string? url { get; set; }
    }

    // model for the DALL E api response
    private class ResponseModel
    {
        public long created { get; set; }
        public List<Link>? data { get; set; }
    }
}