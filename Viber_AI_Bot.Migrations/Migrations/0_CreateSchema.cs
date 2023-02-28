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
                .Nullable())
            .WithColumn("Balance", x => x
                .AsInt32()
                .NotNullable())
            .WithColumn("Language", x => x
                .AsString(10)
                .NotNullable())
            .WithColumn("SelectedMode", x => x
                .AsString(20)
                .NotNullable());
    }

    public override void Down()
    {
        Delete.Table("Users").InSchema("viber");
        // Delete.Schema("viber");
    }
}