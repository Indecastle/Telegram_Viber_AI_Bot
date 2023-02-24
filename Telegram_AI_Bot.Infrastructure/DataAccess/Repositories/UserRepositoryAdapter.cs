using Microsoft.EntityFrameworkCore;
using Telegram_AI_Bot.Core.Models.Users;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Webinex.Coded;

namespace Telegram_AI_Bot.Infrastructure.DataAccess.Repositories;

internal class UserRepositoryAdapter : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepositoryAdapter(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> ExistsAsync(string userId)
    {
        return await _dbContext.Users.AnyAsync(x => x.UserId == userId);
    }

    public async Task AddAsync(User user)
    {
        await _dbContext.AddAsync(user);
    }

    public async Task<User> ByIdAsync(Guid id)
    {
        return await _dbContext.Users.FindAsync(id)
            ?? throw CodedException.NotFound(id);
    }

    public async Task<User[]> ByIdAsync(IEnumerable<Guid> ids)
    {
        ids = ids.Distinct().ToArray();
        if (!ids.Any())
            return Array.Empty<User>();
        
        return await _dbContext.Users.Where(x => ids.Contains(x.Id)).ToArrayAsync();
    }

    public async Task<User[]> AllAsync(string[]? roles)
    {
        var queryable = _dbContext.Users.AsQueryable();

        if (roles?.Any() ?? false)
            queryable = queryable.Where(x => roles.Contains(x.Role.Value));
        
        return await queryable.ToArrayAsync();
    }
}