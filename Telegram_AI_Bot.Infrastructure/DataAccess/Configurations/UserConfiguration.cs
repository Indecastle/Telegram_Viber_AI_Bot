using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Telegram_AI_Bot.Core.Models.Users;

namespace Telegram_AI_Bot.Infrastructure.DataAccess.Configurations;

internal class UserConfiguration : IEntityTypeConfiguration<TelegramUser>
{
    public void Configure(EntityTypeBuilder<TelegramUser> builder)
    {
        builder.ToTable("Users");

        builder.OwnsOne(x => x.Name, e =>
        {
            e.Property(x => x.FirstName).HasColumnName("FirstName");
            e.Property(x => x.LastName).HasColumnName("LastName");
        });
        
        builder.OwnsMany(x => x.Messages, o =>
        {
            o.WithOwner().HasForeignKey("UserId");
            o.ToTable("Users_OpenAiMessages");
            o.HasKey(x => x.Id);
        });

        builder.OwnsOne(x => x.SelectedMode, e => e.Property(x => x.Value).HasColumnName("SelectedMode"));
        builder.OwnsOne(x => x.Role, e => e.Property(x => x.Value).HasColumnName("Role"));
    }
}