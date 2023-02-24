using System;
using FluentMigrator;
using Webinex.Migrations.Extensions;

namespace Telegram_AI_Bot.Migrations;

[Migration(0)]
public class AddUsersTable : Migration 
{
    public override void Up()
    {
        // Create.Schema("viber");
        
        Create
            .Table("Users")
            .InSchema("viber")
            .WithColumn("Id", x => x
                .AsGuid()
                .NotNullable()
                .PrimaryKey())
            .WithColumn("UserId", x => x
                .AsString(100)
                .Unique()
                .NotNullable())
            .WithColumn("Name", x => x
                .AsString(250)
                .NotNullable())
            .WithColumn("Role", x => x
                .AsString(250)
                .NotNullable())
            .WithColumn("Avatar", x=> x
                .AsString(250)
                .Nullable());
    }

    public override void Down()
    {
        Delete.Table("Users").InSchema("viber");
        // Delete.Schema("viber");
    }
}