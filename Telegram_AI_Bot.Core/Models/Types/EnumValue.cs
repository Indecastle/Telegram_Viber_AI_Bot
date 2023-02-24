using MyTemplate.App.Core.Models.Types;

namespace Telegram_AI_Bot.Core.Models.Types;

public abstract class EnumValue<TValue> : ValueObject, ISingleValueObject<TValue> where TValue : notnull
{
    protected abstract HashSet<TValue> PossibleValues { get; }

    public TValue Value { get; protected init; }

    protected EnumValue()
    {
    }

    protected EnumValue(TValue value)
    {
        Asserts.Arg(value).In(PossibleValues);
        Value = value;
    }

    TValue ISingleValueObject<TValue>.Convert() => Value;

    public override string ToString() => Value.ToString()!;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}