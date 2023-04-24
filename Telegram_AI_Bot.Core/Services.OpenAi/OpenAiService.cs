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
    Task<string> ChatHandler(string requestText, IOpenAiUser user);
    IAsyncEnumerable<ChatResponse> GetStreamingChat(string requestText, IOpenAiUser user);
    Task<string?> ImageHandler(string requestText, IOpenAiUser user, ImageSize size = ImageSize.Small);
    ChatRequest GetChatRequest(string requestText, IOpenAiUser user);
}

public class OpenAiService : IOpenAiService
{
    // The following is a conversation with an AI assistant. The assistant is helpful, creative, clever, tells in great detail and very friendly
    private static readonly ChatPrompt[] TemplateSystemChatPrompt3 = { new("system", "You are a helpful assistant.\nYou are Chat GPT-3.5 version") };
    private static readonly ChatPrompt[] TemplateSystemChatPrompt4 = { new("system", "You are a helpful assistant.\nYou are Chat GPT-4 version") };
    public static readonly TikToken TikTokenGPT3Model = TikToken.EncodingForModel("gpt-3.5-turbo");
    private static readonly string[] _stops = {"user", "assistant"};

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

    public async Task<string> ChatHandler(string requestText, IOpenAiUser user)
    {
        requestText = requestText.Trim();
        
        var factorRequest = user.ChatModel == ChatModel.Gpt4 ? _openAiOptions.FactorTextGpt4.Value : 1;
        var factorResponse = user.ChatModel == ChatModel.Gpt4 ? _openAiOptions.FactorTextGpt4.Value*2 : 1;

        var chatRequest = GetChatRequest(requestText, user);
        
        string chatRequestJson = JsonConvert.SerializeObject(chatRequest.Messages);
        int requestTokens = TikTokenGPT3Model.Encode(chatRequestJson).Count * factorRequest;
        CheckEnoughBalance(user, requestTokens, "");
        
        var result = await _api.ChatEndpoint.GetCompletionAsync(chatRequest);
        var resultText = result.FirstChoice.Message.ToString().Trim();
        
        UserContextHandler(user, requestText, resultText);
        user.ReduceChatTokens(result.Usage.PromptTokens*factorRequest + result.Usage.CompletionTokens*factorResponse, _openAiOptions);

        return resultText;
    }

    public async IAsyncEnumerable<ChatResponse> GetStreamingChat(string requestText, IOpenAiUser user)
    {
        requestText = requestText.Trim();

        var chatRequest = GetChatRequest(requestText, user);

        var strBuilder = new StringBuilder();
        
        string chatRequestJson = JsonConvert.SerializeObject(chatRequest.Messages);
        var factorRequest = user.ChatModel == ChatModel.Gpt4 ? _openAiOptions.FactorTextGpt4.Value : 1;
        int requestTokens = TikTokenGPT3Model.Encode(chatRequestJson).Count * factorRequest;
        CheckEnoughBalance(user, requestTokens, "");

        try
        {
            await foreach (var result in _api.ChatEndpoint.StreamCompletionEnumerableAsync(chatRequest))
            {
                strBuilder.Append(result.FirstChoice);
                yield return result;
                CheckEnoughBalance(user, requestTokens, strBuilder.ToString());
            }
        }
        finally
        {
            var factorResponse = user.ChatModel == ChatModel.Gpt4 ? _openAiOptions.FactorTextGpt4.Value*2 : 1;
            int responseTokens = TikTokenGPT3Model.Encode(strBuilder.ToString()).Count * factorResponse;

            UserContextHandler(user, requestText, strBuilder.ToString());
            user.ReduceChatTokens(requestTokens + responseTokens + 1, _openAiOptions);
        }
    }

    private void CheckEnoughBalance(IOpenAiUser user, int requestTokens, string responseText)
    {
        var factorResponse = user.ChatModel == ChatModel.Gpt4 ? _openAiOptions.FactorTextGpt4.Value * 2 : 1;
        int responseTokens = TikTokenGPT3Model.Encode(responseText).Count * factorResponse;

        if (user.Balance < requestTokens + responseTokens)
            throw new NoEnoughBalance(user.Balance < requestTokens);
    }

    public ChatRequest GetChatRequest(string requestText, IOpenAiUser user)
    {
        var newPromptMessage = new ChatPrompt("user", requestText);

        IEnumerable<ChatPrompt> resultDialog = !string.IsNullOrWhiteSpace(user.SystemMessage)
            ? new ChatPrompt[] { new("system", user.SystemMessage) }
            : user.ChatModel == ChatModel.Gpt35
                ? TemplateSystemChatPrompt3
                : TemplateSystemChatPrompt4;

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

class NoEnoughBalance : Exception
{
    public bool IsOnlyOverRequest { get; }

    public NoEnoughBalance(bool isOnlyOverRequest)
    {
        IsOnlyOverRequest = isOnlyOverRequest;
    }

    public NoEnoughBalance(int balance)
        : base($"No Enough Balance: {balance}")
    {

    }
}