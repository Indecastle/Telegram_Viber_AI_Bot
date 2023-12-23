using Askmethat.Aspnet.JsonLocalizer.Localizer;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram_AI_Bot.Core.Models;
using Telegram_AI_Bot.Core.Models.Users;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Services.Telegram.OpenAi;
using Telegram_AI_Bot.Core.Telegram;

namespace Telegram_AI_Bot.Core.Services.Telegram.UpdateEvent;

public interface IBotOnMessageReceivedService
{
    Task BotOnMessageReceived(Message message, bool isEditedMessage, CancellationToken cancellationToken);
    Task BotOnPhotoReceived(Message message, CancellationToken cancellationToken);
}

public class BotOnMessageReceivedService(
        ITelegramBotClient botClient,
        ILogger<BotOnMessageReceivedService> logger,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ITelegramOpenAiService telegramOpenAiService,
        IJsonStringLocalizer localizer,
        IBotOnMessageWaitingService messageWaitingService)
    : IBotOnMessageReceivedService
{
    public async Task BotOnMessageReceived(Message message, bool isEditedMessage, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetOrCreateIfNotExistsAsync(message.From ?? throw new ArgumentNullException());
        TelegramMessageHelper.SetCulture(user.Language);
        LogMessage(message, user);

        if (user.IsTyping)
            return;
        
        if (!isEditedMessage && user.WaitState != null)
        {
            await messageWaitingService.WaitStateHandler(message, user, cancellationToken);
            return;
        }

        if (message is {Type: MessageType.SuccessfulPayment})
        {
            await SuccessfulPaymentHandler(message, user, cancellationToken);
            return;
        }
        
        if (message.Text is not { } messageText)
            return;

        var action = messageText switch
        {
            var x when x.StartsWith("/") => CommandHandler(message, user, cancellationToken),
            _ => telegramOpenAiService.MessageHandler(message, user, cancellationToken)
        };
        
        await action;
    }

    public async Task BotOnPhotoReceived(Message message, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetOrCreateIfNotExistsAsync(message.From ?? throw new ArgumentNullException());
        TelegramMessageHelper.SetCulture(user.Language);
        LogMessage(message, user);

        if (user.IsTyping)
            return;
        
        if (user.ChatModel != ChatModel.Gpt4)
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: localizer.GetString("UploadPhotoNeedGpt4Vision"),
                cancellationToken: cancellationToken);
            return;
        }

        await telegramOpenAiService.UploadingPhotoHandler(message, user, cancellationToken);
    }

    

    private void LogMessage(Message message, TelegramUser user)
    {
        switch (message.Type)
        {
            case MessageType.Text:
                logger.LogInformation("Receive message from user: {UserName} | with text: {Text}", message.From!.Username ?? message.From.FirstName, message.Text);
                break;
            default:
                logger.LogInformation("Receive message type: {Type}, from user: {UserName}", message.Type, message.From!.Username ?? message.From.FirstName);
                break;
        }
    }

    private async Task SuccessfulPaymentHandler(Message message, TelegramUser user, CancellationToken cancellationToken)
    {
        
    }

    private async Task CommandHandler(Message message, TelegramUser user, CancellationToken cancellationToken)
    {
        var args = message.Text.Split(' ');
        string command = args[0];
        args = args.Skip(1).ToArray();

        if (!TelegramCommands.All.Contains(command.ToLowerInvariant()))
            return;
        
        await botClient.SendChatActionAsync(
            chatId: message.Chat.Id,
            chatAction: ChatAction.Typing,
            cancellationToken: cancellationToken);
        
        var action = command switch
        {
            TelegramCommands.Start => StartCommand(message, args, user, cancellationToken),
            TelegramCommands.MainMenu => MainMenuCommand(message, args, user, cancellationToken),
            TelegramCommands.Settings => SettingsCommand(message, args, user, cancellationToken),
            TelegramCommands.Balance => BalanceCommand(message, args, user, cancellationToken),
            TelegramCommands.Help => HelpCommand(message, args, cancellationToken),
        };

        await action;
        
        await unitOfWork.CommitAsync();
    }

    private async Task StartCommand(Message message, string[] args, TelegramUser user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.ChatModel))
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: localizer.GetString("ChooseChatModelBegin"),
                replyMarkup: TelegramInlineMenus.SetChatModelBegin(localizer, user),
                cancellationToken: cancellationToken);
        else
            await MainMenuCommand(message, args, user, cancellationToken);
    }
    
    private async Task MainMenuCommand(Message message, string[] args, TelegramUser user, CancellationToken cancellationToken)
    {
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: localizer.GetString("MainMenu"),
            replyMarkup: TelegramInlineMenus.MainMenu(localizer),
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    private async Task SettingsCommand(Message message, string[] args, TelegramUser user, CancellationToken cancellationToken)
    {
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: TelegramInlineMenus.GetSettingsText(localizer, user),
            replyMarkup: TelegramInlineMenus.SettingsMenu(localizer, user),
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }
    
    private async Task BalanceCommand(Message message, string[] args, TelegramUser user, CancellationToken cancellationToken)
    {
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: localizer.GetString("Balance", user.Balance.ToString("N0")),
            replyMarkup: TelegramInlineMenus.BalanceMenu(localizer),
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }
    
    private async Task HelpCommand(Message message, string[] args, CancellationToken cancellationToken)
    {
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: localizer.GetString("HelpText"),
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }
}