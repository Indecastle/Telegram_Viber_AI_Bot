namespace Telegram_AI_Bot.Core.Models.Types;

public interface ISingleValueObject<T>
{
    T Convert();
}