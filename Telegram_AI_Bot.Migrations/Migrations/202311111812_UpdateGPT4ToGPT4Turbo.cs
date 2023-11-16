using System.Data;
using FluentMigrator;
using Webinex.Migrations.Extensions;

namespace Telegram_AI_Bot.Migrations.Migrations;

[Migration(202311111812)]
public class UpdateGPT4ToGPT4Turbo : Migration
{
    public override void Up()
    {
        Create.Column("Type").OnTable("Users_OpenAiMessages").AsInt32().NotNullable().SetExistingRowsTo(1);
        Create.Column("Type").OnTable("TelegramOpenAiAllMessages").AsInt32().NotNullable().SetExistingRowsTo(1);
        Update.Table("Users").Set(new { ChatModel = "gpt-4-vision-preview" }).Where(new { ChatModel = "gpt-4" });
    }

    public override void Down()
    {
        Delete.Column("Type").FromTable("Users_OpenAiMessages");
        Delete.Column("Type").FromTable("TelegramOpenAiAllMessages");
        Update.Table("Users").Set(new { ChatModel = "gpt-4" }).Where(new { ChatModel = "gpt-4-vision-preview" });
    }
}