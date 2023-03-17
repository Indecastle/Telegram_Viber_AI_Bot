using System;
using FluentMigrator;
using Webinex.Migrations.Extensions;

namespace Telegram_AI_Bot.Migrations.Migrations;

[Migration(0)]
public class AddUsersTable : Migration 
{
    public override void Up()
    {
        Create
            .Table("Users")
            .WithColumn("Id", x => x
                .AsGuid()
                .NotNullable()
                .PrimaryKey())
            .WithColumn("UserId", x => x
                .AsInt64()
                .Unique()
                .NotNullable())
            .WithColumn("FirstName", x => x
                .AsString(250)
                .NotNullable())
            .WithColumn("LastName", x => x
                .AsString(250)
                .Nullable())
            .WithColumn("Role", x => x
                .AsString(250)
                .NotNullable())
            .WithColumn("Avatar", x=> x
                .AsString(250)
                .Nullable())
            .WithColumn("Balance", x => x
                .AsInt64()
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
        Delete.Table("Users");
    }
}