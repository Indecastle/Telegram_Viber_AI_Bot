using MyTemplate.App.Core.Models.Types;
using Telegram_AI_Bot.Core.Models.Types;

namespace Telegram_AI_Bot.Core.Models;

public sealed class WaitState : EnumValue<string>
{
    public WaitState(string value) : base(value)
    {
    }

    protected WaitState()
    {
    }

    public static readonly WaitState SystemMessage = new() { Value = "SystemMessage" };

    protected override HashSet<string> PossibleValues { get; } = new()
    {
        SystemMessage!,
    };

    public static implicit operator string?(WaitState? WaitState)
    {
        return WaitState?.Value;
    }

    public static implicit operator WaitState?(string? WaitState)
    {
        return WaitState != null ? new WaitState(WaitState) : null;
    }

    public static bool operator ==(WaitState? left, WaitState? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(WaitState? left, WaitState? right)
    {
        return NotEqualOperator(left, right);
    }
}