using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram_AI_Bot.Api.Abstract;


namespace Telegram_AI_Bot.Api.Services;

public class ReceiverService : ReceiverServiceBase<UpdateHandler>
{
    public ReceiverService(
        ITelegramBotClient botClient,
        UpdateHandler updateHandler,
        ILogger<ReceiverServiceBase<UpdateHandler>> logger)
        : base(botClient, updateHandler, logger)
    {
    }
}