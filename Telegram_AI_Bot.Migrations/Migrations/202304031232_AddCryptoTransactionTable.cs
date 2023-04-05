using FluentMigrator;
using Webinex.Migrations.Extensions;

namespace Telegram_AI_Bot.Migrations.Migrations;

[Migration(202304031232)]
public class AddCryptoTransactionTable : Migration
{
    public override void Up()
    {
        Create
            .Table("CryptoTransactions")
            .WithColumn("Id", x => x
                .AsGuid()
                .NotNullable()
                .PrimaryKey())
            .WithColumn("UserId", x => x
                .AsGuid()
                .ForeignKey("Users", "Id"))
            .WithColumn("TelegramUserId", x => x
                .AsInt64()
                .NotNullable())
            .WithColumn("InvoiceId", x => x
                .AsInt64()
                .Unique()
                .NotNullable())
            .WithColumn("Currency", x => x
                .AsString(20)
                .NotNullable())
            .WithColumn("Amount", x => x
                .AsDecimal()
                .NotNullable())
            .WithColumn("Tokens", x => x
                .AsInt64()
                .NotNullable())
            .WithColumn("CreatedAt", x => x
                .AsDateTimeOffset()
                .NotNullable())
            .WithColumn("PaidAt", x => x
                .AsDateTimeOffset()
                .NotNullable());
    }

    public override void Down()
    {
        Delete.Table("CryptoTransactions");
    }
}