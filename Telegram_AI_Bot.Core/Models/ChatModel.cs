using MyTemplate.App.Core.Models.Types;
using Telegram_AI_Bot.Core.Models.Types;

namespace Telegram_AI_Bot.Core.Models;

public sealed class ChatModel : EnumValue<string>
{
    public ChatModel(string value) : base(value)
    {
    }

    protected ChatModel()
    {
    }

    public static readonly ChatModel Gpt35 = new() { Value = "gpt-3.5-turbo-1106" };
    public static readonly ChatModel Gpt4 = new() { Value = "gpt-4-vision-preview" };
    
    public static HashSet<string> All { get; } = new()
    {
        Gpt35!,
        Gpt4!,
    };

    protected override HashSet<string> PossibleValues { get; } = new()
    {
        Gpt35!,
        Gpt4!,
    };

    public static implicit operator string?(ChatModel? ChatModel)
    {
        return ChatModel?.Value;
    }

    public static implicit operator ChatModel?(string? ChatModel)
    {
        return ChatModel != null ? new ChatModel(ChatModel) : null;
    }

    public static bool operator ==(ChatModel? left, ChatModel? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(ChatModel? left, ChatModel? right)
    {
        return NotEqualOperator(left, right);
    }
}