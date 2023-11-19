using Askmethat.Aspnet.JsonLocalizer.Localizer;
using Microsoft.Extensions.Localization;
using Telegram_AI_Bot.Core.Models;
using Telegram_AI_Bot.Core.Models.Users;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Telegram;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Telegram_AI_Bot.Core.Services.Telegram.UpdateEvent;

public interface IBotOnMessageWaitingService
{
    Task WaitStateHandler(Message message, TelegramUser user, CancellationToken cancellationToken);
}
    
public class BotOnMessageWaitingService(
        ITelegramBotClient botClient,
        IJsonStringLocalizer localizer,
        IUnitOfWork unitOfWork) 
    : IBotOnMessageWaitingService
{
    
    public async Task WaitStateHandler(Message message, TelegramUser user, CancellationToken cancellationToken)
    {
        var action = user.WaitState switch
        {
            var x when x == WaitState.SystemMessage => ChangeSystemMessage(message, user, cancellationToken),
            _ => ResetWaitState(message, user, cancellationToken)
        };
        
        await action;
    }

    private async Task ChangeSystemMessage(Message message, TelegramUser user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.Text) || message.Text.StartsWith("/"))
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: localizer.GetString("SystemMessageMenu.InvalidateChange"),
                cancellationToken: cancellationToken);
            return;
        }
        user.SetSystemMessage(message.Text);
        user.ResetWaitState();
        await unitOfWork.CommitAsync();
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: localizer.GetString("SystemMessageMenu.HasBeenChanged"),
            replyMarkup: TelegramInlineMenus.BackPrevMenu(localizer,
                TelegramCommands.Keyboard.Settings_SystemMessage),
            cancellationToken: cancellationToken);
    }
    
    private async Task ResetWaitState(Message message, TelegramUser user, CancellationToken cancellationToken)
    {
        user.ResetWaitState();
        await unitOfWork.CommitAsync();
    }
}