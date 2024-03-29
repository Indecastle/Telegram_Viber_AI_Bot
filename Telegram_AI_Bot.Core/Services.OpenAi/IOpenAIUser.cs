using OpenAI.Images;
using Telegram_AI_Bot.Core.Models;

namespace Telegram_AI_Bot.Core.Services.OpenAi;

public interface IOpenAiUser
{
    long Balance { get; }
    string? SystemMessage { get; }
    ChatModel? ChatModel { get; }
    SelectedMode SelectedMode { get; set; }
    bool IsTyping { get; }
    public DateTimeOffset? LastTypingAt { get; }
    IReadOnlyCollection<OpenAiMessage> Messages { get; }
    bool IsPositiveBalance();
    bool ClearContext();
    void AddMessage(string text, bool isMe, DateTimeOffset time);
    void RemoveUnnecessary();
    void ReduceChatTokens(int tokens, OpenAiConfiguration openAiOptions);
    void ReduceImageTokens(ImageSize imageSize, OpenAiConfiguration openAiOptions);
    void SetBalance(int amount);
    void SetChatModel(ChatModel model);
    void SetLanguage(string lang);
    void SwitchMode();
    bool IsEnabledContext();
    bool IsEnabledStreamingChat();
}