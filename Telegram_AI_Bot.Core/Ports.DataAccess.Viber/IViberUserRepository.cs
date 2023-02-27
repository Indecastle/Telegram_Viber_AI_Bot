using Telegram_AI_Bot.Core.Models.Users;
using Telegram_AI_Bot.Core.Models.Viber.Users;
using InternalViberUser = Viber.Bot.NetCore.Models.ViberUser.User;

namespace Telegram_AI_Bot.Core.Ports.DataAccess.Viber;

public interface IViberUserRepository
{
    Task AddAsync(ViberUser user);
    Task<bool> ExistsAsync(string userId);
    Task<ViberUser> GetOrCreateIfNotExistsAsync(InternalViberUser user);
    Task<ViberUser> ByUserIdAsync(string userId);
    Task<ViberUser> ByIdAsync(Guid id);
    Task<ViberUser[]> ByIdAsync(IEnumerable<Guid> ids);
    Task<ViberUser[]> AllAsync(string[]? roles);
}