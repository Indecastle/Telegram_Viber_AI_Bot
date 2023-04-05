using CryptoPay.Types;
using MyTemplate.App.Core.Models.Types;
using OpenAI.Images;
using Telegram.Bot.Types;
using Telegram_AI_Bot.Core.Common;
using Telegram_AI_Bot.Core.Models.Types;
using Telegram_AI_Bot.Core.Services.OpenAi;

namespace Telegram_AI_Bot.Core.Models.Users;

public class CryptoTransaction : IEntity, IAggregatedRoot, IHasId
{
    protected CryptoTransaction()
    {
    }

    public Guid Id { get; protected set; }
    public Guid UserId { get; protected set; }
    public long TelegramUserId { get; protected set; }
    public long InvoiceId { get; protected set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public long Tokens { get; set; }
    public DateTimeOffset CreatedAt { get; protected set; }
    public DateTimeOffset PaidAt { get; protected set; }
    
    public static CryptoTransaction Create(
        Invoice invoice, TelegramUser user, Assets currency, long tokens)
    {
        return New(user.Id, user.UserId, invoice.InvoiceId, (decimal)invoice.Amount, currency, tokens, invoice.CreatedAt, invoice.PaidAt!.Value);
    }
    
    private static CryptoTransaction New(
        Guid userId,
        long telegramUserId,
        long invoiceId,
        decimal amount,
        Assets currency,
        long tokens,
        DateTimeOffset createdAt,
        DateTimeOffset paidAt)
    {
        var user = new CryptoTransaction
        {
            Id = Guid.NewGuid(),
            
            UserId = userId,
            TelegramUserId = telegramUserId,
            InvoiceId = invoiceId,
            Amount = amount,
            Currency = currency.ToString(),
            Tokens = tokens,
            CreatedAt = createdAt,
            PaidAt = paidAt,
        };
        return user;
    }
}