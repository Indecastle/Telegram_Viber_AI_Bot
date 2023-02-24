using System;
using FluentMigrator;
using Webinex.Migrations.Extensions;

namespace Telegram_AI_Bot.Migrations;

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
                .AsString(20)
                .Unique()
                .NotNullable())
            .WithColumn("FirstName", x => x
                .AsString(250)
                .NotNullable())
            .WithColumn("LastName", x => x
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
        Delete.Table("Users");
    }
}