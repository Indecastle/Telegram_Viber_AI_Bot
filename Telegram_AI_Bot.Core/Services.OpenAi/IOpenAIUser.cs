using OpenAI.Images;
using Telegram_AI_Bot.Core.Models;

namespace Telegram_AI_Bot.Core.Services.OpenAi;

public interface IOpenAiUser
{
    long Balance { get; set; }
    SelectedMode SelectedMode { get; set; }
    IReadOnlyCollection<OpenAiMessage> Messages { get; }
    bool IsPositiveBalance();
    void DeleteContext();
    void AddMessage(string text, bool isMe, DateTimeOffset time);
    void RemoveUnnecessary();
    void ReduceChatTokens(int tokens);
    void ReduceImageTokens(ImageSize imageSize, OpenAiConfiguration openAiOptions);



    void SetLanguage(string lang);

    void SwitchMode();
}