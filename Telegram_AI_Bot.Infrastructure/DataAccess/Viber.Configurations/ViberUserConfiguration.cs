﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Telegram_AI_Bot.Core.Models.Users;
using Telegram_AI_Bot.Core.Models.Viber.Users;

namespace Telegram_AI_Bot.Infrastructure.DataAccess.Viber.Configurations;

internal class ViberUserConfiguration : IEntityTypeConfiguration<ViberUser>
{
    public void Configure(EntityTypeBuilder<ViberUser> builder)
    {
        {
            builder.ToTable("Users", schema: "viber");

            builder.OwnsOne(x => x.Role, e => e.Property(x => x.Value).HasColumnName("Role"));
        }
    }
}