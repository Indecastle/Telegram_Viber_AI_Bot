using FluentMigrator;
using Webinex.Migrations.Extensions;

namespace Telegram_AI_Bot.Migrations.Migrations;

[Migration(202303251415)]
public class AddUserNameColumnToTable : Migration
{
    public override void Up()
    {
        Create.Column("UserName").OnTable("Users").AsString(64).Nullable();
        Create.Column("StartAt").OnTable("Users").AsDateTimeOffset().NotNullable().SetExistingRowsTo(DateTimeOffset.UtcNow);
    }

    public override void Down()
    {
        Delete.Column("UserName").FromTable("Users");
        Delete.Column("StartAt").FromTable("Users");
    }
}