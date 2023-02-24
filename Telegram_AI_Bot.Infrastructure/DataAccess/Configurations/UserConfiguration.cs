using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Telegram_AI_Bot.Core.Models.Users;

namespace Telegram_AI_Bot.Infrastructure.DataAccess.Configurations;

internal class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.OwnsOne(x => x.Name, e =>
        {
            e.Property(x => x.FirstName).HasColumnName("FirstName");
            e.Property(x => x.LastName).HasColumnName("LastName");
        });

        builder.OwnsOne(x => x.Role, e => e.Property(x => x.Value).HasColumnName("Role"));
    }
}