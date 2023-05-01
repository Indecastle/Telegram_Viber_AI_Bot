using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;
using Telegram_AI_Bot.Core.Common;
using Telegram_AI_Bot.Core.Models;
using Telegram_AI_Bot.Core.Models.Users;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Telegram;
using Webinex.Coded;
// using User = Telegram_AI_Bot.Core.Models.Users.User;

namespace Telegram_AI_Bot.Infrastructure.DataAccess.Repositories;

internal class UserRepositoryAdapter : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepositoryAdapter(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> ExistsAsync(long userId)
    {
        return await _dbContext.Users.AnyAsync(x => x.UserId == userId);
    }
    
    public async Task<TelegramUser> GetOrCreateIfNotExistsAsync(User internalUser)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.UserId == internalUser!.Id);
        if (user == null)
        {
            var lang = TelegramMessageHelper.SetDefaultCulture(internalUser.LanguageCode);
            var entryEntity = await _dbContext.Users.AddAsync(
                await TelegramUser.NewClientAsync(
                    internalUser.Id,
                    internalUser.Username,
                    new Name(internalUser.FirstName, internalUser.LastName),
                    lang,
                    Constants.FREE_START_BALANCE,
                    false));

            await _dbContext.SaveChangesAsync();
            return entryEntity.Entity;
        }

        if (user.IsNeedUpdateBaseInfo(internalUser))
        {
            user.UpdateBaseInfo(internalUser);
            await _dbContext.SaveChangesAsync();
        }

        return user;
    }

    public async Task AddAsync(TelegramUser telegramUser)
    {
        await _dbContext.AddAsync(telegramUser);
    }

    public async Task<TelegramUser> ByUserIdAsync(long userId)
    {
        return await _dbContext.Users.Include(x => x.Messages).FirstOrDefaultAsync(x => x.UserId == userId)
               ?? throw CodedException.NotFound(userId);
    }

    public async Task<TelegramUser> ByIdAsync(Guid id)
    {
        return await _dbContext.Users.FindAsync(id)
            ?? throw CodedException.NotFound(id);
    }

    public async Task<TelegramUser[]> ByIdAsync(IEnumerable<Guid> ids)
    {
        ids = ids.Distinct().ToArray();
        if (!ids.Any())
            return Array.Empty<TelegramUser>();
        
        return await _dbContext.Users.Where(x => ids.Contains(x.Id)).ToArrayAsync();
    }

    public async Task<TelegramUser[]> AllAsync(string[]? roles)
    {
        var queryable = _dbContext.Users.AsQueryable();

        if (roles?.Any() ?? false)
            queryable = queryable.Where(x => roles.Contains(x.Role.Value));
        
        return await queryable.ToArrayAsync();
    }

    public async Task<TelegramUser[]> GetAllWithLowBalance()
    {
        return await _dbContext.Users.AsQueryable()
            .Where(x => x.Balance < Constants.LOW_LIMIT_BALANCE)
            .ToArrayAsync();
    }

    public async Task<TelegramUser[]> GetAllByUserId(long[] userIds)
    {
        return await _dbContext.Users.AsQueryable()
            .Where(x => !x.IsTyping && userIds.Contains(x.UserId))
            .ToArrayAsync();
    }

    public async Task<TelegramUser[]> GetAllTyping()
    {
        return await _dbContext.Users.AsQueryable()
            .Where(x => x.IsTyping)
            .ToArrayAsync();
    }
}