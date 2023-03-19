using FluentMigrator;
using Webinex.Migrations.Extensions;

namespace Telegram_AI_Bot.Migrations.Migrations;

[Migration(202303251315)]
public class AddEnabledStreamingColumnToTable : Migration
{
    public override void Up()
    {
        Create.Column("EnabledStreamingChat").OnTable("Users").AsBoolean().NotNullable().SetExistingRowsTo(false);
    }

    public override void Down()
    {
        Delete.Column("EnabledStreamingChat").FromTable("Users");
    }
}