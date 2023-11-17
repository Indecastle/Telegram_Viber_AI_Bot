using System.Data;
using FluentMigrator;
using Webinex.Migrations.Extensions;

namespace Telegram_AI_Bot.Migrations.Migrations;

[Migration(202311171209)]
public class UpdateGPT3TurboToGPT3Turbo1106 : Migration
{
    public override void Up()
    {
        Update.Table("Users").Set(new { ChatModel = "gpt-3.5-turbo-1106" }).Where(new { ChatModel = "gpt-3.5-turbo" });
    }

    public override void Down()
    {
        Update.Table("Users").Set(new { ChatModel = "gpt-3.5-turbo" }).Where(new { ChatModel = "gpt-3.5-turbo-1106" });
    }
}