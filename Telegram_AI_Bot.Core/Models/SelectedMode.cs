using MyTemplate.App.Core.Models.Types;
using Telegram_AI_Bot.Core.Models.Types;

namespace Telegram_AI_Bot.Core.Models;

public sealed class SelectedMode : EnumValue<string>
{
    public SelectedMode(string value) : base(value)
    {
    }

    protected SelectedMode()
    {
    }

    public static readonly SelectedMode Chat = new() { Value = "Chat" };
    public static readonly SelectedMode Image = new() { Value = "Image" };

    public SelectedMode NextMode => this == Chat ? Image : Chat;

    protected override HashSet<string> PossibleValues { get; } = new()
    {
        Chat!,
        Image!,
    };

    public static implicit operator string?(SelectedMode? selectedMode)
    {
        return selectedMode?.Value;
    }

    public static implicit operator SelectedMode?(string? selectedMode)
    {
        return selectedMode != null ? new SelectedMode(selectedMode) : null;
    }

    public static bool operator ==(SelectedMode? left, SelectedMode? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(SelectedMode? left, SelectedMode? right)
    {
        return NotEqualOperator(left, right);
    }
}