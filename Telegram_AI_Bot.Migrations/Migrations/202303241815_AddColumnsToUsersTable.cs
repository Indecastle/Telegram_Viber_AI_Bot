using FluentMigrator;
using Webinex.Migrations.Extensions;

namespace Telegram_AI_Bot.Migrations;

[Migration(202303241815)]
public class AddColumnsToUsersTable : Migration
{
    public override void Up()
    {
        Alter
            .Table("Users")
            .AddColumn("Balance", x => x
                .AsInt32()
                .NotNullable()
                .SetExistingRowsTo(0));
            
    }
    
    public override void Down()
    {
        Delete.Column("Balance").FromTable("Users");
    }
}