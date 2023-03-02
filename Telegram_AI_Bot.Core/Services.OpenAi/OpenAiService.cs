using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Completions;
using OpenAI.Images;
using OpenAI.Models;
using Telegram_AI_Bot.Core.Models.Viber.Users;
using InternalViberUser = Viber.Bot.NetCore.Models.ViberUser.User;

namespace Telegram_AI_Bot.Core.Services.OpenAi;

public interface IOpenAiService
{
    Task<string?> ChatHandler(string requestText, IOpenAiUser user);
    Task<string?> ImageHandler(string requestText, IOpenAiUser user);
}

public class OpenAiService : IOpenAiService
{
    // The following is a conversation with an AI assistant. The assistant is helpful, creative, clever, tells in great detail and very friendly
    private static readonly ChatPrompt[] TemplateSystemChatPrompt = { new("system", "You are a helpful assistant.") };

    private readonly OpenAiConfiguration _openAiptions;
    private readonly OpenAIClient _api;

    public OpenAiService(
        IOptions<OpenAiConfiguration> openAiptions)
    {
        _openAiptions = openAiptions.Value;
        _api = new OpenAIClient(new OpenAIAuthentication(_openAiptions.Token, null));
    }

    public async Task<string?> ChatHandler(string requestText, IOpenAiUser user)
    {
        var now = DateTimeOffset.UtcNow;
        requestText = requestText.Trim();

        var newMessage = new ChatPrompt("user", requestText);
        
        IEnumerable<ChatPrompt> dialog = user.Messages.OrderBy(x => x.CreatedAt).ThenBy(x => x.IsMe)
            .TakeWhile(x => x.CreatedAt < now)
            .Select(x => new ChatPrompt(x.IsMe ? "user" : "assistant", x.Text)).ToArray();

        var resultDialog = TemplateSystemChatPrompt.Concat(dialog).Concat(new[] { newMessage });
        var chatRequest = new ChatRequest(resultDialog);

        var result = await _api.ChatEndpoint.GetCompletionAsync(chatRequest);

        var text = result.Choices[0].Message.ToString().Trim();
        
        user.AddMessage(requestText, true, now);
        user.AddMessage(text, false, now);
        user.RemoveUnnecessary();
        
        return text;
    }

    public async Task<string?> ImageHandler(string requestText, IOpenAiUser user)
    {
        var images = await GetImages(requestText.Trim());
        return images.FirstOrDefault();
    }
    
    private async Task<string[]> GetImages(string prompt, int numberOfResults = 1, ImageSize size = ImageSize.Small)
    {
        var results = await _api.ImagesEndPoint.GenerateImageAsync(prompt, numberOfResults, size);
        return results.ToArray();
    }
}