using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;
using Telegram_AI_Bot.Core.Common;
using Telegram_AI_Bot.Core.Models;
using Telegram_AI_Bot.Core.Models.Users;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Telegram;
using Webinex.Coded;
// using User = Telegram_AI_Bot.Core.Models.Users.User;

namespace Telegram_AI_Bot.Infrastructure.DataAccess.Repositories;

internal class CryptoTransactionRepositoryAdapter : ICryptoTransactionRepository
{
    private readonly AppDbContext _dbContext;

    public CryptoTransactionRepositoryAdapter(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> ExistsAsync(long invoiceId)
    {
        return await _dbContext.CryptoTransactions.AnyAsync(x => x.InvoiceId == invoiceId);
    }

    public async Task<long[]> NoExistsInvoiceIds(long[] invoiceIds)
    {
        var existsIds = await _dbContext.CryptoTransactions.Select(x => x.InvoiceId).Where(x => invoiceIds.Contains(x)).ToArrayAsync();
        return invoiceIds.Except(existsIds).ToArray();
    }

    public async Task AddRangeAsync(CryptoTransaction[] invoices)
    {
        await _dbContext.CryptoTransactions.AddRangeAsync(invoices);
    }
}