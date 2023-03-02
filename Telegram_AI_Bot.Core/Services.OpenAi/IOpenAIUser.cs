using Telegram_AI_Bot.Core.Models;

namespace Telegram_AI_Bot.Core.Services.OpenAi;

public interface IOpenAiUser
{
    public IReadOnlyCollection<OpenAiMessage> Messages { get; }

    public void DeleteContext();
    public void AddMessage(string text, bool isMe, DateTimeOffset time);
    public void RemoveUnnecessary();
}