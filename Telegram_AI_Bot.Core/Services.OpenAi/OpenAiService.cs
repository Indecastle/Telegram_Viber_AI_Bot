using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Images;
using InternalViberUser = Viber.Bot.NetCore.Models.ViberUser.User;

namespace Telegram_AI_Bot.Core.Services.OpenAi;

public interface IOpenAiService
{
    Task<string?> ChatHandler(string requestText, IOpenAiUser user);
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
        var now = DateTimeOffset.UtcNow;
        requestText = requestText.Trim();

        var chatRequest = GetChatRequest(requestText, user);
        var result = await _api.ChatEndpoint.GetCompletionAsync(chatRequest);

        var text = result.Choices[0].Message.ToString().Trim();

        if (user.IsEnabledContext())
        {
            user.AddMessage(requestText, true, now);
            user.AddMessage(text, false, now);
            user.RemoveUnnecessary();
        }

        user.ReduceChatTokens(result.Usage.TotalTokens);

        return text;
    }

    public ChatRequest GetChatRequest(string requestText, IOpenAiUser user)
    {
        var newPromptMessage = new ChatPrompt("user", requestText);

        IEnumerable<ChatPrompt> resultDialog = TemplateSystemChatPrompt;

        if (user.IsEnabledContext())
            resultDialog = resultDialog.Concat(
                user.Messages.OrderBy(x => x.CreatedAt).ThenBy(x => x.IsMe)
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
}