using FluentMigrator;
using Webinex.Migrations.Extensions;

namespace Telegram_AI_Bot.Migrations.Migrations;

[Migration(202303251215)]
public class AddEnabledContextColumnToTable : Migration
{
    public override void Up()
    {
        Create.Column("EnabledContext").OnTable("Users").AsBoolean().NotNullable().SetExistingRowsTo(true);
    }

    public override void Down()
    {
        Delete.Column("EnabledContext").FromTable("Users");
    }
}