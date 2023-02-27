using FluentMigrator;
using Webinex.Migrations.Extensions;

namespace Telegram_AI_Bot.Migrations;

[Migration(202303241815)]
public class AddColumnsToUsersTable : Migration
{
    public override void Up()
    {
        Alter
            .Table("Users").InSchema("viber")
            .AddColumn("Balance", x => x
                .AsInt32()
                .NotNullable()
                .SetExistingRowsTo(0))
            .AddColumn("Language", x => x
                .AsString(10)
                .NotNullable())
            .AddColumn("MessageHistory", x => x
                .AsString()
                .NotNullable())
            .AddColumn("SelectedMode", x => x
                .AsString(20)
                .NotNullable());
            
    }

    public override void Down()
    {
        Delete.Column("Balance").FromTable("Users").InSchema("viber");
        Delete.Column("Language").FromTable("Users").InSchema("viber");
        Delete.Column("MessageHistory").FromTable("Users").InSchema("viber");
        Delete.Column("SelectedMode").FromTable("Users").InSchema("viber");
    }
}