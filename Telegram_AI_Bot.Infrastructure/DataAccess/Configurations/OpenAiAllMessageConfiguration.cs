using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Telegram_AI_Bot.Core.Models;
using Telegram_AI_Bot.Core.Models.Users;

namespace Telegram_AI_Bot.Infrastructure.DataAccess.Configurations;

internal class OpenAiAllMessageConfiguration : IEntityTypeConfiguration<OpenAiAllMessage>
{
    public void Configure(EntityTypeBuilder<OpenAiAllMessage> builder)
    {
        builder.ToTable("TelegramOpenAiAllMessages");
        builder.HasOne<TelegramUser>().WithMany().HasForeignKey(e => e.UserId);
    }
}