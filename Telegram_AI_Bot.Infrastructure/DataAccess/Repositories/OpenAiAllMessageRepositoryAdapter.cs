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

internal class OpenAiAllMessageRepositoryAdapter : IOpenAiAllMessageRepository
{
    private readonly AppDbContext _dbContext;

    public OpenAiAllMessageRepositoryAdapter(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task AddAsync(OpenAiAllMessage message)
    {
        await _dbContext.TelegramOpenAiAllMessages.AddAsync(message);
    }

    public async Task AddRangeAsync(OpenAiAllMessage[] messages)
    {
        await _dbContext.TelegramOpenAiAllMessages.AddRangeAsync(messages);
    }
}