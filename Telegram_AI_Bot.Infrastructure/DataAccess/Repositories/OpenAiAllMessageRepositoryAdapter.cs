using Telegram_AI_Bot.Core.Models;
using Telegram_AI_Bot.Core.Ports.DataAccess;

// using User = Telegram_AI_Bot.Core.Models.Users.User;

namespace Telegram_AI_Bot.Infrastructure.DataAccess.Repositories;

internal class OpenAiAllMessageRepositoryAdapter : IOpenAiAllMessageRepository
{
    private readonly AppDbContext _dbContext;

    public OpenAiAllMessageRepositoryAdapter(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddRangeAsync(OpenAiAllMessage[] messages)
    {
        await _dbContext.TelegramOpenAiAllMessages.AddRangeAsync(messages);
    }
}