using FluentMigrator;
using Webinex.Migrations.Extensions;

namespace Telegram_AI_Bot.Migrations;

[Migration(202303241815)]
public class AddColumnsToUsersTable : Migration
{
    public override void Up()
    {
        Create
            .Table("Users_OpenAiMessages")
            .InSchema("viber")
            .WithColumn("Id", x => x
                .AsGuid()
                .NotNullable()
                .PrimaryKey())
            .WithColumn("UserId", x => x
                .AsGuid()
                .ForeignKey("FK_Users_OpenAiMessages_UserId_viber.Users_Id", "viber", "Users", "Id")
                .NotNullable())
            .WithColumn("Text", x => x
                .AsString(1000)
                .NotNullable())
            .WithColumn("IsMe", x => x
                .AsBoolean()
                .NotNullable())
            .WithColumn("CreatedAt", x => x
                .AsDateTimeOffset()
                .NotNullable());

    }

    public override void Down()
    {
        Delete.Table("Users_OpenAiMessages").InSchema("viber");
    }
}