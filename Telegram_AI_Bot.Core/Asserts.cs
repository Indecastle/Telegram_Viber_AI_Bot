using System.Collections;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Telegram_AI_Bot.Core.Models;
using Webinex.Coded;

namespace Telegram_AI_Bot.Core;

public static class Asserts
{
    public static ArgumentInfo<T> Arg<T>(T value, [CallerArgumentExpression("value")] string paramName = "")
    {
        return new ArgumentInfo<T>(value, paramName);
    }

    [Obsolete("Use Assert.Arg(value) instead")]
    public static ArgumentInfo<T> Arg<T>(Expression<Func<T>> memberExpression)
    {
        return memberExpression.Body is MemberExpression m
            ? Arg(memberExpression.Compile()(), m.Member.Name)
            : throw new ArgumentException("A member expression is expected.", nameof(memberExpression));
    }

    public class ArgumentInfo<T>
    {
        public T Value { get; private set; }
        public string ArgumentName { get; private set; }

        public ArgumentInfo(T value, string paramName)
        {
            Value = value;
            ArgumentName = paramName;
        }
    }
}

public static class ArgumentInfoExtensions
{
    public static Asserts.ArgumentInfo<T> NotNull<T>(this Asserts.ArgumentInfo<T> argumentInfo)
        where T : class?
    {
        return argumentInfo.Assert(argumentInfo.Value != null, $"{argumentInfo.ArgumentName} cannot be null");
    }

    public static Asserts.ArgumentInfo<T?> NotNull<T>(this Asserts.ArgumentInfo<T?> argumentInfo)
        where T : struct
    {
        return argumentInfo.Assert(argumentInfo.Value.HasValue, $"{argumentInfo.ArgumentName} cannot be null");
    }

    public static Asserts.ArgumentInfo<T?> Null<T>(this Asserts.ArgumentInfo<T?> argumentInfo)
        where T : struct
    {
        return argumentInfo.Assert(!argumentInfo.Value.HasValue, $"{argumentInfo.ArgumentName} should be null");
    }

    public static Asserts.ArgumentInfo<T?> Null<T>(this Asserts.ArgumentInfo<T?> argumentInfo)
        where T : class?
    {
        return argumentInfo.Assert(argumentInfo.Value == null, $"{argumentInfo.ArgumentName} should be null");
    }

    public static Asserts.ArgumentInfo<Guid> NotEmpty(this Asserts.ArgumentInfo<Guid> argumentInfo)
    {
        return argumentInfo.Assert(argumentInfo.Value != default, $"{argumentInfo.ArgumentName} cannot be empty");
    }

    public static Asserts.ArgumentInfo<Guid?> NotEmpty(this Asserts.ArgumentInfo<Guid?> argumentInfo)
    {
        if (!argumentInfo.Value.HasValue)
        {
            return argumentInfo;
        }

        return argumentInfo.Assert(argumentInfo.Value.Value != default, $"{argumentInfo.ArgumentName} cannot be empty");
    }

    public static Asserts.ArgumentInfo<T> NotEmpty<T>(this Asserts.ArgumentInfo<T> argumentInfo)
        where T : class, IEnumerable
    {
        if (argumentInfo.Value == null)
        {
            return argumentInfo;
        }

        return argumentInfo.Assert(argumentInfo.Value.Cast<object>().Any(),
            $"{argumentInfo.ArgumentName} cannot be empty");
    }

    public static Asserts.ArgumentInfo<string> InFormat(
        this Asserts.ArgumentInfo<string> argumentInfo,
        Regex regex)
    {
        if (argumentInfo.Value == null)
        {
            return argumentInfo;
        }

        return argumentInfo.Assert(regex.IsMatch(argumentInfo.Value),
            $"{argumentInfo.ArgumentName} should be in format {regex}");
    }

    public static Asserts.ArgumentInfo<string> NotNullOrWhiteSpace(this Asserts.ArgumentInfo<string> argumentInfo)
    {
        return argumentInfo
            .NotNull()
            .Assert(!string.IsNullOrWhiteSpace(argumentInfo.Value),
                $"{argumentInfo.ArgumentName} cannot be null or white space");
    }

    public static Asserts.ArgumentInfo<string> LengthLessThanOrEqual(
        this Asserts.ArgumentInfo<string> argumentInfo,
        int length)
    {
        if (argumentInfo.Value == null)
        {
            return argumentInfo;
        }

        return argumentInfo.Assert(argumentInfo.Value.Length <= length, $"{argumentInfo.ArgumentName} is too long");
    }

    public static Asserts.ArgumentInfo<string> SmLength(
        this Asserts.ArgumentInfo<string> argumentInfo)
    {
        return LengthLessThanOrEqual(argumentInfo, ModelSettings.SmField);
    }

    public static Asserts.ArgumentInfo<string> MdLength(
        this Asserts.ArgumentInfo<string> argumentInfo)
    {
        return LengthLessThanOrEqual(argumentInfo, ModelSettings.MdField);
    }

    public static Asserts.ArgumentInfo<string> LgLength(
        this Asserts.ArgumentInfo<string> argumentInfo)
    {
        return LengthLessThanOrEqual(argumentInfo, ModelSettings.LgField);
    }

    public static Asserts.ArgumentInfo<string> XlLength(
        this Asserts.ArgumentInfo<string> argumentInfo)
    {
        return LengthLessThanOrEqual(argumentInfo, ModelSettings.XlField);
    }

    public static Asserts.ArgumentInfo<int?> GreaterThanOrEqual(
        this Asserts.ArgumentInfo<int?> argumentInfo,
        int min)
    {
        if (argumentInfo.Value.HasValue)
        {
            argumentInfo.Assert(argumentInfo.Value >= min, $"{argumentInfo.ArgumentName} is too small");
        }

        return argumentInfo;
    }

    public static Asserts.ArgumentInfo<double> NotNegative(this Asserts.ArgumentInfo<double> argumentInfo)
    {
        return argumentInfo.Assert(argumentInfo.Value >= 0, $"{argumentInfo.ArgumentName} is negative");
    }

    public static Asserts.ArgumentInfo<int> NotNegative(this Asserts.ArgumentInfo<int> argumentInfo)
    {
        return argumentInfo.Assert(argumentInfo.Value >= 0, $"{argumentInfo.ArgumentName} is negative");
    }

    public static Asserts.ArgumentInfo<int> GreaterThan(this Asserts.ArgumentInfo<int> argumentInfo, int min)
    {
        return argumentInfo.Assert(argumentInfo.Value > min, $"{argumentInfo.ArgumentName} is too small");
    }

    public static Asserts.ArgumentInfo<int> GreaterThanOrEqual(
        this Asserts.ArgumentInfo<int> argumentInfo,
        int min)
    {
        return argumentInfo.Assert(argumentInfo.Value >= min, $"{argumentInfo.ArgumentName} is too small");
    }

    public static Asserts.ArgumentInfo<decimal> GreaterThanOrEqual(
        this Asserts.ArgumentInfo<decimal> argumentInfo,
        decimal min)
    {
        return argumentInfo.Assert(argumentInfo.Value >= min, $"{argumentInfo.ArgumentName} is too small");
    }

    public static Asserts.ArgumentInfo<DateTime?> GreaterThanOrEqual(
        this Asserts.ArgumentInfo<DateTime?> argumentInfo,
        DateTime min)
    {
        if (argumentInfo.Value.HasValue)
        {
            argumentInfo.Assert(argumentInfo.Value >= min, $"{argumentInfo.ArgumentName} is too small");
        }

        return argumentInfo;
    }

    public static Asserts.ArgumentInfo<int> LessThan(this Asserts.ArgumentInfo<int> argumentInfo, int max)
    {
        return argumentInfo.Assert(argumentInfo.Value < max, $"{argumentInfo.ArgumentName} is too big");
    }

    public static Asserts.ArgumentInfo<int> LessThanOrEqual(this Asserts.ArgumentInfo<int> argumentInfo, int max)
    {
        return argumentInfo.Assert(argumentInfo.Value <= max, $"{argumentInfo.ArgumentName} is too big");
    }

    public static Asserts.ArgumentInfo<double> LessThanOrEqual(
        this Asserts.ArgumentInfo<double> argumentInfo,
        double max)
    {
        return argumentInfo.Assert(argumentInfo.Value <= max, $"{argumentInfo.ArgumentName} is too big");
    }

    public static Asserts.ArgumentInfo<int?> LessThanOrEqual(this Asserts.ArgumentInfo<int?> argumentInfo, int max)
    {
        if (argumentInfo.Value.HasValue)
        {
            argumentInfo.Assert(argumentInfo.Value <= max, $"{argumentInfo.ArgumentName} is too big");
        }

        return argumentInfo;
    }

    public static Asserts.ArgumentInfo<T> In<T>(this Asserts.ArgumentInfo<T> argumentInfo, params T[] values)
    {
        return argumentInfo.Assert(values.Contains(argumentInfo.Value),
            $"{argumentInfo.ArgumentName} is not in valid values");
    }

    public static Asserts.ArgumentInfo<T> In<T>(this Asserts.ArgumentInfo<T> argumentInfo, IEnumerable<T> values)
    {
        return argumentInfo.Assert(values.Contains(argumentInfo.Value),
            $"{argumentInfo.ArgumentName} is not in valid values");
    }

    public static Asserts.ArgumentInfo<T> Condition<T>(
        this Asserts.ArgumentInfo<T> argumentInfo,
        Func<T, bool> condition)
    {
        return argumentInfo.Assert(condition(argumentInfo.Value),
            $"{argumentInfo.ArgumentName} doesn't match condition");
    }

    public static Asserts.ArgumentInfo<T> If<T>(
        this Asserts.ArgumentInfo<T> argumentInfo,
        bool ifCondition,
        Action<Asserts.ArgumentInfo<T>> than)
    {
        if (ifCondition)
        {
            than(argumentInfo);
        }

        return argumentInfo;
    }

    public static Asserts.ArgumentInfo<bool> True(this Asserts.ArgumentInfo<bool> argumentInfo)
    {
        return argumentInfo.Assert(argumentInfo.Value, $"{argumentInfo.ArgumentName} should be true");
    }

    public static Asserts.ArgumentInfo<T> Not<T>(this Asserts.ArgumentInfo<T> argumentInfo, T value)
    {
        return argumentInfo.Assert(!ReferenceEquals(argumentInfo.Value, value) && argumentInfo.Value?.Equals(value) == false,
            $"{argumentInfo!.ArgumentName} should not be {value}");
    }

    public static Asserts.ArgumentInfo<T> ToBe<T>(this Asserts.ArgumentInfo<T> argumentInfo, T value)
    {
        return argumentInfo.Assert(ReferenceEquals(argumentInfo.Value, value) || argumentInfo.Value?.Equals(value) == true,
            $"{argumentInfo!.ArgumentName} should be {value}");
    }

    private static Asserts.ArgumentInfo<T> Assert<T>(
        this Asserts.ArgumentInfo<T> argumentInfo,
        bool condition,
        string message)
    {
        if (!condition)
        {
            throw CodedException.Invalid(message);
        }

        return argumentInfo;
    }
}