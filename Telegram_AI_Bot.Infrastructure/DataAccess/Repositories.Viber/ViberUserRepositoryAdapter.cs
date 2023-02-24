using Microsoft.EntityFrameworkCore;
using Telegram_AI_Bot.Core.Models;
using Telegram_AI_Bot.Core.Models.Users;
using Telegram_AI_Bot.Core.Models.Viber.Users;
using Telegram_AI_Bot.Core.Ports.DataAccess.Viber;
using InternalViberUser = Viber.Bot.NetCore.Models.ViberUser.User;
using Webinex.Coded;

namespace Telegram_AI_Bot.Infrastructure.DataAccess.Repositories.Viber;

internal class ViberUserRepositoryAdapter : IViberUserRepository
{
    public const int FREE_START_BALANCE = 20;
    
    private readonly AppDbContext _dbContext;

    public ViberUserRepositoryAdapter(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ViberUser user)
    {
        await _dbContext.AddAsync(user);
    }
    
    public async Task<bool> ExistsAsync(string userId)
    {
        return await _dbContext.ViberUser.AnyAsync(x => x.UserId == userId);
    }

    public async Task<bool> CreateNewIfNotExistsAsync(InternalViberUser user)
    {
        if (!await ExistsAsync(user.Id))
        {
            await _dbContext.ViberUser.AddAsync(await ViberUser.NewClientAsync(user.Id, user.Name, FREE_START_BALANCE));
            await _dbContext.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<ViberUser> ByIdAsync(Guid id)
    {
        return await _dbContext.ViberUser.FindAsync(id)
            ?? throw CodedException.NotFound(id);
    }
    
    public async Task<ViberUser> ByUserIdAsync(string userId)
    {
        return await _dbContext.ViberUser.FirstOrDefaultAsync(x => x.UserId == userId)
               ?? throw CodedException.NotFound(userId);
    }

    public async Task<ViberUser[]> ByIdAsync(IEnumerable<Guid> ids)
    {
        ids = ids.Distinct().ToArray();
        if (!ids.Any())
            return Array.Empty<ViberUser>();
        
        return await _dbContext.ViberUser.Where(x => ids.Contains(x.Id)).ToArrayAsync();
    }

    public async Task<ViberUser[]> AllAsync(string[]? roles)
    {
        var queryable = _dbContext.ViberUser.AsQueryable();

        if (roles?.Any() ?? false)
            queryable = queryable.Where(x => roles.Contains(x.Role.Value));
        
        return await queryable.ToArrayAsync();
    }
}