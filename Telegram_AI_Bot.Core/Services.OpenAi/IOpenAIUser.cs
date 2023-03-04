using OpenAI.Images;
using Telegram_AI_Bot.Core.Models;

namespace Telegram_AI_Bot.Core.Services.OpenAi;

public interface IOpenAiUser
{
    public IReadOnlyCollection<OpenAiMessage> Messages { get; }

    public void DeleteContext();
    public void AddMessage(string text, bool isMe, DateTimeOffset time);
    public void RemoveUnnecessary();
    public void ReduceChatTokens(int tokens);
    public void ReduceImageTokens(ImageSize imageSize, OpenAiConfiguration openAiOptions);
}