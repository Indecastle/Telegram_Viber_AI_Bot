using OpenAI.Chat;
using Telegram_AI_Bot.Core.Models;
using Telegram_AI_Bot.Core.Telegram;
using Role = OpenAI.Chat.Role;

namespace Telegram_AI_Bot.Tests;

public class TelegramMessageHelperTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void GetNumTokensFromMessages_WhenGPT4()
    {
        var messages = new Message[]
        {
            new(Role.System, "System Message"),
            new(Role.User, "Hello"),
            new(Role.Assistant, "Hello2"),
        };
        var result = TelegramMessageHelper.GetNumTokensFromMessages(ChatModel.Gpt4, messages);
        Assert.That(result, Is.EqualTo(20));
    }
    
    [Test]
    public void GetNumTokensFromMessages_WhenGPT35()
    {
        var messages = new Message[]
        {
            new(Role.System, "System Message"),
            new(Role.User, "Hello"),
            new(Role.Assistant, "Hello2"),
        };
        var result = TelegramMessageHelper.GetNumTokensFromMessages(ChatModel.Gpt35, messages);
        Assert.That(result, Is.EqualTo(23));
    }
}