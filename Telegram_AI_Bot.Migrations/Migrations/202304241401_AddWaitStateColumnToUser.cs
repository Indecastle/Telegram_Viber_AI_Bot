using System.Data;
using FluentMigrator;
using Webinex.Migrations.Extensions;

namespace Telegram_AI_Bot.Migrations.Migrations;

[Migration(202304241401)]
public class AddWaitStateColumnToUser : Migration
{
    public override void Up()
    {
        Create.Column("WaitState").OnTable("Users").AsString(250).Nullable();
        Create.Column("SystemMessage").OnTable("Users").AsString(int.MaxValue).Nullable();
    }

    public override void Down()
    {
        Delete.Column("WaitState").FromTable("Users");
        Delete.Column("SystemMessage").FromTable("Users");
    }
}