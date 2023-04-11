using System.Data;
using FluentMigrator;
using Webinex.Migrations.Extensions;

namespace Telegram_AI_Bot.Migrations.Migrations;

[Migration(202304111342)]
public class AddAllMessagesTable : Migration
{
    public override void Up()
    {
        Create
            .Table("TelegramOpenAiAllMessages")
            .WithColumn("Id", x => x
                .AsGuid()
                .NotNullable()
                .PrimaryKey())
            .WithColumn("UserId", x => x
                .AsGuid()
                .ForeignKey("Users", "Id").OnDelete(Rule.Cascade))
            .WithColumn("TelegramUserId", x => x
                .AsInt64()
                .NotNullable())
            .WithColumn("Text", x => x
                .AsString(int.MaxValue)
                .NotNullable())
            .WithColumn("IsMe", x => x
                .AsBoolean()
                .NotNullable())
            .WithColumn("CreatedAt", x => x
                .AsDateTimeOffset()
                .NotNullable());
        
        Delete.ForeignKey().FromTable("Users_OpenAiMessages").ForeignColumn("UserId").ToTable("Users").PrimaryColumn("Id");
        Alter.Column("UserId").OnTable("Users_OpenAiMessages").AsGuid().ForeignKey("Users", "Id").OnDelete(Rule.Cascade);
    }

    public override void Down()
    {
        Delete.Table("TelegramOpenAiAllMessages");
    }
}