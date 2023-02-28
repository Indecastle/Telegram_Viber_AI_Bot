using MyTemplate.App.Core.Models.Types;

namespace Telegram_AI_Bot.Core.Models;

public class OpenAiMessage : ValueObject
{
    protected OpenAiMessage()
    {
    }

    public OpenAiMessage(Guid id, string text, bool isMe, DateTimeOffset createdAt)
    {
        Id = id;
        Text = text;
        IsMe = isMe;
        CreatedAt = createdAt;
    }
    
    public Guid Id { get; protected set; }
    public string Text { get; protected set; }
    public bool IsMe { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected set; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Id;
        yield return Text;
        yield return IsMe;
        yield return CreatedAt;
    }

    public static bool operator ==(OpenAiMessage? left, OpenAiMessage? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(OpenAiMessage? left, OpenAiMessage? right)
    {
        return NotEqualOperator(left, right);
    }
}