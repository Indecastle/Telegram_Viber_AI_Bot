using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Images;
using OpenAI.Models;
using Telegram_AI_Bot.Core.Models;
using TiktokenSharp;
using InternalViberUser = Viber.Bot.NetCore.Models.ViberUser.User;

namespace Telegram_AI_Bot.Core.Services.OpenAi;

public interface IOpenAiService
{
    Task<string?> ChatHandler(string requestText, IOpenAiUser user);
    IAsyncEnumerable<ChatResponse> GetStreamingChat(string requestText, IOpenAiUser user);
    Task<string?> ImageHandler(string requestText, IOpenAiUser user, ImageSize size = ImageSize.Small);
}

public class OpenAiService : IOpenAiService
{
    // The following is a conversation with an AI assistant. The assistant is helpful, creative, clever, tells in great detail and very friendly
    private static readonly ChatPrompt[] TemplateSystemChatPrompt = { new("system", "You are a helpful assistant.\nYou are Chat GPT-4 version") };
    private static readonly TikToken _tikToken = TikToken.EncodingForModel("gpt-3.5-turbo");

    private readonly OpenAiConfiguration _openAiOptions;
    private readonly OpenAIClient _api;
    private readonly IDateTimeProvider _dateTimeProvider;

    public OpenAiService(
        IOptions<OpenAiConfiguration> openAiOptions,
        IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
        _openAiOptions = openAiOptions.Value;
        _api = new OpenAIClient(new OpenAIAuthentication(_openAiOptions.Token, _openAiOptions.OrganizationId));
    }

    public async Task<string?> ChatHandler(string requestText, IOpenAiUser user)
    {
        requestText = requestText.Trim();

        var chatRequest = GetChatRequest(requestText, user);
        var result = await _api.ChatEndpoint.GetCompletionAsync(chatRequest);
        var resultText = result.FirstChoice.Message.ToString().Trim();
        
        // string jsonString = JsonConvert.SerializeObject(chatRequest.Messages);
        UserContextHandler(user, requestText, resultText);
        user.ReduceChatTokens(result.Usage.TotalTokens, _openAiOptions);

        return resultText;
    }

    public async IAsyncEnumerable<ChatResponse> GetStreamingChat(string requestText, IOpenAiUser user)
    {
        requestText = requestText.Trim();

        var chatRequest = GetChatRequest(requestText, user);

        var strBuilder = new StringBuilder();

        try
        {
            await foreach (var result in _api.ChatEndpoint.StreamCompletionEnumerableAsync(chatRequest))
            {
                strBuilder.Append(result.FirstChoice);
                yield return result;
            }
        }
        finally
        {
            string chatRequestJson = JsonConvert.SerializeObject(chatRequest.Messages);
            int tokens1 = _tikToken.Encode(chatRequestJson).Count;
            int tokens2 = _tikToken.Encode(strBuilder.ToString()).Count;

            var mulResponse = user.ChatModel == ChatModel.Gpt4 ? _openAiOptions.FactorTextGpt4.Value : 1;

            UserContextHandler(user, requestText, strBuilder.ToString());
            user.ReduceChatTokens(tokens1 + tokens2*mulResponse + 1, _openAiOptions);
        }
    }

    public ChatRequest GetChatRequest(string requestText, IOpenAiUser user)
    {
        var newPromptMessage = new ChatPrompt("user", requestText);

        IEnumerable<ChatPrompt> resultDialog = TemplateSystemChatPrompt;

        if (user.IsEnabledContext())
            resultDialog = resultDialog.Concat(
                user.Messages.OrderBy(x => x.CreatedAt).ThenByDescending(x => x.IsMe)
                    .TakeWhile(x => x.CreatedAt < _dateTimeProvider.UtcNow)
                    .Select(x => new ChatPrompt(x.IsMe ? "user" : "assistant", x.Text)).ToArray());

        resultDialog = resultDialog.Concat(new[] { newPromptMessage });

        return new ChatRequest(resultDialog, model: user.ChatModel!.Value);
    }

    public async Task<string?> ImageHandler(string requestText, IOpenAiUser user, ImageSize size = ImageSize.Small)
    {
        var images = await GetImages(requestText.Trim());
        user.ReduceImageTokens(size, _openAiOptions);
        return images.FirstOrDefault();
    }

    private async Task<string[]> GetImages(string prompt, int numberOfResults = 1, ImageSize size = ImageSize.Small)
    {
        var results = await _api.ImagesEndPoint.GenerateImageAsync(prompt, numberOfResults, size);
        return results.ToArray();
    }

    public void UserContextHandler(IOpenAiUser user, string requestText, string resultText)
    {
        var now = _dateTimeProvider.UtcNow;
        
        if (user.IsEnabledContext())
        {
            user.AddMessage(requestText, true, now);
            user.AddMessage(resultText, false, now);
            user.RemoveUnnecessary();
        }
    }
}