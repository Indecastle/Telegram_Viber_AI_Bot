using MyTemplate.App.Core.Models.Types;

namespace Telegram_AI_Bot.Core.Models;

public class Name : ValueObject
{
    public Name(string firstName, string? lastName)
    {
        Asserts.Arg(firstName).NotNullOrWhiteSpace().MdLength();

        FirstName = firstName;
        LastName = lastName;
    }

    protected Name()
    {
    }

    [FieldLength.Md]
    public string FirstName { get; protected set; } = null!;

    [FieldLength.Md]
    public string? LastName { get; protected set; } = null!;

    public string FullName() => $"{FirstName} {LastName}";

    public static bool operator ==(Name? left, Name? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(Name? left, Name? right)
    {
        return NotEqualOperator(left, right);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FirstName;
        yield return LastName;
    }
}