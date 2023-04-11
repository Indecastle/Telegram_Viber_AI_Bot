using Microsoft.EntityFrameworkCore;
using Telegram_AI_Bot.Core.Models;
using Telegram_AI_Bot.Core.Models.Users;
using Telegram_AI_Bot.Core.Models.Viber.Users;

namespace Telegram_AI_Bot.Infrastructure.DataAccess;

internal class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<TelegramUser> Users { get; set; } = null!;
    public DbSet<CryptoTransaction> CryptoTransactions { get; set; } = null!;
    public DbSet<OpenAiAllMessage> TelegramOpenAiAllMessages { get; set; } = null!;
    public DbSet<ViberUser> ViberUser { get; set; } = null!;

    public async Task SaveChangesAsync()
    {
        await SaveChangesAsync(CancellationToken.None);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InfrastructureModule).Assembly);

        base.OnModelCreating(modelBuilder);
    }
    
}