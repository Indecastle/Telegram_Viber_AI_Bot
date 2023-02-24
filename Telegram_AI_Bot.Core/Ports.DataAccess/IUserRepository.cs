using Telegram_AI_Bot.Core.Models.Users;

namespace Telegram_AI_Bot.Core.Ports.DataAccess;

public interface IUserRepository
{
    Task<bool> ExistsAsync(string email);
    Task AddAsync(User user);
    Task<User> ByIdAsync(Guid id);
    Task<User[]> ByIdAsync(IEnumerable<Guid> ids);
    Task<User[]> AllAsync(string[]? roles);
}