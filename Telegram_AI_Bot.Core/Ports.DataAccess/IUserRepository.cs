using Telegram.Bot.Types;
using Telegram_AI_Bot.Core.Models.Users;

namespace Telegram_AI_Bot.Core.Ports.DataAccess;

public interface IUserRepository
{
    Task<bool> ExistsAsync(long email);
    Task<TelegramUser> GetOrCreateIfNotExistsAsync(User user);
    Task AddAsync(TelegramUser telegramUser);
    Task<TelegramUser> ByUserIdAsync(long userId);
    Task<TelegramUser> ByIdAsync(Guid id);
    Task<TelegramUser[]> ByIdAsync(IEnumerable<Guid> ids);
    Task<TelegramUser[]> AllAsync(string[]? roles);
    Task<TelegramUser[]> GetAllWithLowBalance();
    Task<TelegramUser[]> GetAllByUserId(long[] userIds);
    Task<TelegramUser[]> GetAllTyping();
}