using FluentMigrator;
using Webinex.Migrations.Extensions;

namespace Telegram_AI_Bot.Migrations.Migrations;

[Migration(202303251015)]
public class IncreaseLimitOfTextColumn : Migration
{
    public override void Up()
    {
        Alter
            .Column("Text")
            .OnTable("Users_OpenAiMessages")
            .AsString(10000)
            .NotNullable();
    }

    public override void Down()
    {
        Alter
            .Column("Text")
            .OnTable("Users_OpenAiMessages")
            .AsString(1000)
            .NotNullable();
    }
}