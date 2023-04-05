using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Telegram_AI_Bot.Core.Models.Users;

namespace Telegram_AI_Bot.Infrastructure.DataAccess.Configurations;

internal class CryptoTransactionConfiguration : IEntityTypeConfiguration<CryptoTransaction>
{
    public void Configure(EntityTypeBuilder<CryptoTransaction> builder)
    {
        builder.ToTable("CryptoTransactions");

        builder.HasOne<TelegramUser>().WithMany().HasForeignKey(e => e.UserId);
        builder.Property(x => x.Amount).HasPrecision(26, 8);
    }
}