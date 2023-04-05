namespace Telegram_AI_Bot.Infrastructure;

public static class DictionaryExtensions
{
    public static TValue GetOrDefault<TValue, TKey>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue)) {
        if (dictionary.TryGetValue(key, out var result))
            return result;

        return defaultValue;
    }
}