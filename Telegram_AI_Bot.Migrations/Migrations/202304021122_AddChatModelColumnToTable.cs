using FluentMigrator;
using Webinex.Migrations.Extensions;

namespace Telegram_AI_Bot.Migrations.Migrations;

[Migration(202304021122)]
public class AddChatModelColumnToTable : Migration
{
    public override void Up()
    {
        Create.Column("ChatModel").OnTable("Users").AsString(250).Nullable();
    }

    public override void Down()
    {
        Delete.Column("ChatModel").FromTable("Users");
    }
}