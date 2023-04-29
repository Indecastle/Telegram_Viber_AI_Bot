using System.Data;
using FluentMigrator;
using Webinex.Migrations.Extensions;

namespace Telegram_AI_Bot.Migrations.Migrations;

[Migration(202304291316)]
public class AddIsTypingColumnToUser : Migration
{
    public override void Up()
    {
        Create.Column("IsTyping").OnTable("Users").AsBoolean().NotNullable().SetExistingRowsTo(false);
        Create.Column("LastTypingAt").OnTable("Users").AsDateTimeOffset().Nullable();
    }

    public override void Down()
    {
        Delete.Column("IsTyping").FromTable("Users");
        Delete.Column("LastTypingAt").FromTable("Users");
    }
}