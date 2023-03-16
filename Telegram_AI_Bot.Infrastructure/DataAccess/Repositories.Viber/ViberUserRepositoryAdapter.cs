using Microsoft.EntityFrameworkCore;
using Telegram_AI_Bot.Core.Common;
using Telegram_AI_Bot.Core.Models;
using Telegram_AI_Bot.Core.Models.Users;
using Telegram_AI_Bot.Core.Models.Viber.Users;
using Telegram_AI_Bot.Core.Ports.DataAccess.Viber;
using Telegram_AI_Bot.Core.Services.OpenAi;
using Telegram_AI_Bot.Core.Viber;
using InternalViberUser = Viber.Bot.NetCore.Models.ViberUser.User;
using Webinex.Coded;

namespace Telegram_AI_Bot.Infrastructure.DataAccess.Repositories.Viber;

internal class ViberUserRepositoryAdapter : IViberUserRepository
{
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

    public async Task<ViberUser> GetOrCreateIfNotExistsAsync(InternalViberUser internalUser)
    {
        var user = await _dbContext.ViberUser.FirstOrDefaultAsync(x => x.UserId == internalUser.Id);
        if (user == null)
        {
            ViberMessageHelper.SetDefaultCulture(internalUser.Country);
            var lang = Thread.CurrentThread.CurrentUICulture.Name;
            var entryEntity = await _dbContext.ViberUser.AddAsync(
                await ViberUser.NewClientAsync(internalUser.Id, internalUser.Name, lang, Constants.FREE_START_BALANCE));

            await _dbContext.SaveChangesAsync();
            return entryEntity.Entity;
        }

        return user;
    }

    public async Task<ViberUser> ByIdAsync(Guid id)
    {
        return await _dbContext.ViberUser.FindAsync(id)
            ?? throw CodedException.NotFound(id);
    }
    
    public async Task<ViberUser> ByUserIdAsync(string userId)
    {
        return await _dbContext.ViberUser.Include(x => x.Messages).FirstOrDefaultAsync(x => x.UserId == userId)
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

    public async Task<IOpenAiUser[]> GetAllWithLowBalance()
    {
        return await _dbContext.ViberUser.AsQueryable()
            .Where(x => x.Balance < Constants.LOW_LIMIT_BALANCE)
            .ToArrayAsync();
    }
}