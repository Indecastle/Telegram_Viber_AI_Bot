using Telegram.Bot.Types;
using Telegram_AI_Bot.Core.Models;
using Telegram_AI_Bot.Core.Models.Users;

namespace Telegram_AI_Bot.Core.Ports.DataAccess;

public interface IOpenAiAllMessageRepository
{
    Task AddAsync(OpenAiAllMessage message);
    Task AddRangeAsync(OpenAiAllMessage[] invoices);
}