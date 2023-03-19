using System.Text;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Images;
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
    private static readonly ChatPrompt[] TemplateSystemChatPrompt = { new("system", "You are a helpful assistant.") };

    private readonly OpenAiConfiguration _openAiOptions;
    private readonly OpenAIClient _api;
    private readonly IDateTimeProvider _dateTimeProvider;

    public OpenAiService(
        IOptions<OpenAiConfiguration> openAiOptions,
        IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
        _openAiOptions = openAiOptions.Value;
        _api = new OpenAIClient(new OpenAIAuthentication(_openAiOptions.Token, null));
    }

    public async Task<string?> ChatHandler(string requestText, IOpenAiUser user)
    {
        requestText = requestText.Trim();

        var chatRequest = GetChatRequest(requestText, user);
        var result = await _api.ChatEndpoint.GetCompletionAsync(chatRequest);

        var resultText = result.Choices[0].Message.ToString().Trim();

        UserContextHandler(user, requestText, resultText);
        user.ReduceChatTokens(result.Usage.TotalTokens);

        return resultText;
    }

    public async IAsyncEnumerable<ChatResponse> GetStreamingChat(string requestText, IOpenAiUser user)
    {
        requestText = requestText.Trim();

        var chatRequest = GetChatRequest(requestText, user);

        var strBuilder = new StringBuilder();
        var tokens = 0;

        await foreach (var result in _api.ChatEndpoint.StreamCompletionEnumerableAsync(chatRequest))
        {
            strBuilder.Append(result.FirstChoice);
            tokens += result.Usage?.TotalTokens ?? 0;
            yield return result;
        }

        UserContextHandler(user, requestText, strBuilder.ToString());
        user.ReduceChatTokens(tokens);
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

        return new ChatRequest(resultDialog);
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