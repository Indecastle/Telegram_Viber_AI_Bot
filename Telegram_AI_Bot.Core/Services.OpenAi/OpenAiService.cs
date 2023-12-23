using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Images;
using Telegram_AI_Bot.Core.Models;
using Telegram_AI_Bot.Core.Telegram;
using Telegram.Bot.Types.Enums;
using TiktokenSharp;
using InternalViberUser = Viber.Bot.NetCore.Models.ViberUser.User;
using Role = OpenAI.Role;

namespace Telegram_AI_Bot.Core.Services.OpenAi;

public interface IOpenAiService
{
    Task<ChatResponse> ChatHandler(string requestText, IOpenAiUser user, CancellationToken cancellationToken);
    IAsyncEnumerable<ChatResponse> GetStreamingChat(string requestText, IOpenAiUser user, CancellationToken cancellationToken);
    Task<string?> ImageHandler(string requestText, IOpenAiUser user, ImageSize size = ImageSize.Small);
    ChatRequest GetChatRequest(string requestText, IOpenAiUser user);
}

public class OpenAiService : IOpenAiService
{
    // The following is a conversation with an AI assistant. The assistant is helpful, creative, clever, tells in great detail and very friendly
    private static readonly Message[] TemplateSystemChatPrompt3 = { new(Role.System, "You are a helpful assistant.\nYou are Chat GPT-3.5-Turbo version") };
    private static readonly Message[] TemplateSystemChatPrompt4 = { new(Role.System, "You are a helpful assistant.\nYou are Chat GPT-4-Turbo version") };
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

    public async Task<ChatResponse> ChatHandler(string requestText, IOpenAiUser user, CancellationToken cancellationToken)
    {
        requestText = requestText.Trim();
        
        var factorRequest = user.ChatModel == ChatModel.Gpt4 ? _openAiOptions.FactorTextGpt4.Value : 1;
        var factorResponse = user.ChatModel == ChatModel.Gpt4 ? _openAiOptions.FactorTextGpt4.Value*3 : 1;

        var chatRequest = GetChatRequest(requestText, user);
        
        var requestTokens = TelegramMessageHelper.GetNumTokensFromMessages(user.ChatModel!, chatRequest.Messages) * factorRequest;
        requestTokens += TelegramMessageHelper.GetNumTokensFromPhotos(user.ChatModel!, chatRequest.Messages);
        CheckEnoughBalance(user, requestTokens, "");
        
        var result = await _api.ChatEndpoint.GetCompletionAsync(chatRequest, cancellationToken);
        var resultText = result.FirstChoice.Message.ToString().Trim();

        UserContextHandler(user, requestText, resultText);
        user.ReduceChatTokens(result.Usage.PromptTokens.Value*factorRequest + result.Usage.CompletionTokens.Value*factorResponse, _openAiOptions);

        return result;
    }

    public async IAsyncEnumerable<ChatResponse> GetStreamingChat(string requestText, IOpenAiUser user, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        requestText = requestText.Trim();

        var chatRequest = GetChatRequest(requestText, user);

        var strBuilder = new StringBuilder();
        
        var factorRequest = user.ChatModel == ChatModel.Gpt4 ? _openAiOptions.FactorTextGpt4.Value : 1;
        int requestTokens = TelegramMessageHelper.GetNumTokensFromMessages(user.ChatModel!, chatRequest.Messages) * factorRequest;
        CheckEnoughBalance(user, requestTokens, "");

        try
        {
            await foreach (var result in _api.ChatEndpoint.StreamCompletionEnumerableAsync(chatRequest, cancellationToken))
            {
                if (result.FirstChoice.FinishReason == "stop" || result.FirstChoice.FinishDetails?.Type != null)
                    yield break;

                strBuilder.Append(result.FirstChoice);
                yield return result;
                CheckEnoughBalance(user, requestTokens, strBuilder.ToString());
            }
        }
        finally
        {
            var factorResponse = user.ChatModel == ChatModel.Gpt4 ? _openAiOptions.FactorTextGpt4.Value*3 : 2;
            int responseTokens = TelegramMessageHelper.TikTokenModel.Encode(strBuilder.ToString()).Count * factorResponse;

            UserContextHandler(user, requestText, strBuilder.ToString());
            user.ReduceChatTokens(requestTokens + responseTokens + 1, _openAiOptions);
        }
    }

    private void CheckEnoughBalance(IOpenAiUser user, int requestTokens, string responseText)
    {
        var factorResponse = user.ChatModel == ChatModel.Gpt4 ? _openAiOptions.FactorTextGpt4.Value * 3 : 2;
        int responseTokens = TikTokenGPT3Model.Encode(responseText).Count * factorResponse;

        if (user.Balance < requestTokens + responseTokens)
            throw new NoEnoughBalance(user.Balance < requestTokens);
    }

    public ChatRequest GetChatRequest(string requestText, IOpenAiUser user)
    {
        var newPromptMessage = new Message(Role.User, requestText);

        IEnumerable<Message> resultDialog = !string.IsNullOrWhiteSpace(user.SystemMessage)
            ? new Message[] { new(Role.System, user.SystemMessage) }
            : user.ChatModel == ChatModel.Gpt35
                ? TemplateSystemChatPrompt3
                : TemplateSystemChatPrompt4;

        if (user.IsEnabledContext())
            resultDialog = resultDialog.Concat(
                user.Messages.OrderBy(x => x.CreatedAt).ThenByDescending(x => x.IsMe)
                    .TakeWhile(x => x.CreatedAt < _dateTimeProvider.UtcNow)
                    .Where(x => x.Type == MessageType.Text || user.ChatModel == ChatModel.Gpt4 && x.Type == MessageType.Photo )
                    .Select(x => new Message(x.IsMe ? Role.User : Role.Assistant, new List<Content>
                    {
                        new Content(x.Type == MessageType.Photo ? ContentType.ImageUrl : ContentType.Text, x.Text),
                    })).ToArray());

        resultDialog = resultDialog.Concat(new[] { newPromptMessage });
        
        var tools = new List<Tool>
        {
            new Function(
                "get_image_based_user_prompt",
                "Get a generated image based on the user's last request only if the user strictly and explicitly requests that image. (for example, create a cat). Analyze only the last user request",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["prompt"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "user's prompt only if the user strictly and explicitly requests that image"
                        },
                    },
                    ["required"] = new JsonArray { "prompt" }
                })
        };

        return new ChatRequest(resultDialog,
            model: user.ChatModel!.Value,
            // tools: user.ChatModel == ChatModel.Gpt35 ? tools : null,
            maxTokens: user.ChatModel == ChatModel.Gpt4 ? 4000 : 2000);
    }

    public async Task<string?> ImageHandler(string requestText, IOpenAiUser user, ImageSize size = ImageSize.Small)
    {
        var images = await GetImages(requestText.Trim());
        user.ReduceImageTokens(size, _openAiOptions);
        return images.FirstOrDefault()?.Url;
    }

    private async Task<ImageResult[]> GetImages(string prompt, int numberOfResults = 1, ImageSize size = ImageSize.Small)
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