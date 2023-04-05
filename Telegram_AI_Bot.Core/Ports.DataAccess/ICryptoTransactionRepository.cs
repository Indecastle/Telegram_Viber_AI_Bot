using Telegram.Bot.Types;
using Telegram_AI_Bot.Core.Models.Users;

namespace Telegram_AI_Bot.Core.Ports.DataAccess;

public interface ICryptoTransactionRepository
{
    Task<bool> ExistsAsync(long invoiceId);
    Task<long[]> NoExistsInvoiceIds(long[] invoiceIds);
    Task AddRangeAsync(CryptoTransaction[] invoices);
}