using MyTemplate.App.Core.Models.Types;
using Telegram_AI_Bot.Core.Models.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram_AI_Bot.Core.Models;

public class OpenAiAllMessage : ValueObject
{
    protected OpenAiAllMessage()
    {
    }

    public OpenAiAllMessage(Guid id, Guid userId, long telegramUserId, string text, MessageType type, bool isMe, DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        TelegramUserId = telegramUserId;
        Text = text;
        Type = type;
        IsMe = isMe;
        CreatedAt = createdAt;
    }
    
    public Guid Id { get; protected set; }
    public Guid UserId { get; protected set; }
    public long TelegramUserId { get; protected set; }
    public string Text { get; protected set; }
    public MessageType Type { get; protected set; }
    public bool IsMe { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected set; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Id;
        yield return UserId;
        yield return Text;
        yield return Type;
        yield return IsMe;
        yield return CreatedAt;
    }

    public static bool operator ==(OpenAiAllMessage? left, OpenAiAllMessage? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(OpenAiAllMessage? left, OpenAiAllMessage? right)
    {
        return NotEqualOperator(left, right);
    }
}