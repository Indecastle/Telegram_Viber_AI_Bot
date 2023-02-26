using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Telegram_AI_Bot.Infrastructure.Events.Common;

internal interface IEventStore
{
    bool Any { get; }

    IDbContextTransaction BeginTransaction<TDbContext>(TDbContext dbContext)
        where TDbContext : DbContext;

    Task FlushAsync();
}