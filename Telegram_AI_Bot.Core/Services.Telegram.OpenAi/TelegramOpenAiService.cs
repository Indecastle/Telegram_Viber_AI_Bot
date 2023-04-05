using System.Text;
using Askmethat.Aspnet.JsonLocalizer.Localizer;
using Microsoft.Extensions.Localization;
using MoreLinq.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram_AI_Bot.Core.Models;
using Telegram_AI_Bot.Core.Models.Users;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Ports.DataAccess.Viber;
using Telegram_AI_Bot.Core.Services.OpenAi;
using Telegram_AI_Bot.Core.Telegram;
using Telegram_AI_Bot.Core.Viber;

namespace Telegram_AI_Bot.Core.Services.Telegram.OpenAi;

public interface ITelegramOpenAiService
{
    Task Handler(Message? message, TelegramUser user, CancellationToken cancellationToken);
}

public class TelegramOpenAiService : ITelegramOpenAiService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOpenAiService _openAiService;
    private readonly IJsonStringLocalizer _localizer;

    private static readonly int _streamDelayMilliseconds = 2000;

    public TelegramOpenAiService(
        ITelegramBotClient botClient,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IOpenAiService openAiService,
        IJsonStringLocalizer localizer)
    {
        _botClient = botClient;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _openAiService = openAiService;
        _localizer = localizer;
    }

    public async Task Handler(Message? message, TelegramUser user, CancellationToken cancellationToken)
    {
        await _botClient.SendChatActionAsync(
            chatId: message.Chat.Id,
            chatAction: ChatAction.Typing,
            cancellationToken: cancellationToken);
        
        if (!user.IsPositiveBalance())
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: _localizer.GetString("NoBalance"),
                // replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(user.ChatModel))
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: _localizer.GetString("ChooseChatModelBegin"),
                replyMarkup: TelegramInlineMenus.SetChatModelBegin(_localizer, user),
                cancellationToken: cancellationToken);
            return;
        }

        if (user.SelectedMode == SelectedMode.Chat)
        {
            if (user.IsEnabledStreamingChat())
                try
                {
                    await SendGradually(message, user, cancellationToken);
                }
                finally
                {
                    await _unitOfWork.CommitAsync();
                }
            else
                await SendImmediately(message, user, cancellationToken);
        }
        else
        {
            var url = await _openAiService.ImageHandler(message.Text, user);

            await _botClient.SendPhotoAsync(
                chatId: message.Chat.Id,
                photo: new InputFileUrl(url),
                cancellationToken: cancellationToken);
        }
    }

    private async Task SendImmediately(Message message, TelegramUser storedUser, CancellationToken cancellationToken)
    {
        var waitMessage = await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: _localizer.GetString("Wait"),
            // replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
        
        var textResult = await _openAiService.ChatHandler(message.Text, storedUser);

        if (string.IsNullOrEmpty(textResult))
            await _botClient.EditMessageTextAsync(
                chatId: message.Chat.Id,
                messageId: waitMessage.MessageId,
                text: "bad request",
                cancellationToken: cancellationToken);
        else
            await _unitOfWork.CommitAsync();

        await SendTextMessage(message, waitMessage.MessageId, textResult, cancellationToken);
    }

    private async Task SendTextMessage(Message? message, int waitMessageId, string? textResult, CancellationToken cancellationToken)
    {
        var chunks = textResult!.Batch(4096)
            .Select(x => new string(x.ToArray()))
            .ToArray();
        
        for (int i = 0; i < chunks.Length; i++)
        {
            var text = chunks[i];
                
            if (i == 0)
                await _botClient.EditMessageTextAsync(
                    chatId: message.Chat.Id,
                    messageId: waitMessageId,
                    text: text,
                    cancellationToken: cancellationToken);
            else
                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: text,
                    cancellationToken: cancellationToken);

            if (i != chunks.Length-1)
                await Task.Delay(1000, cancellationToken);
        }
    }
    
    private async Task SendGradually(Message message, TelegramUser storedUser, CancellationToken cancellationToken)
    {
        Message? waitMessage = null;
        var strBuilder = new StringBuilder();
        var strBuilderBuff = new StringBuilder();
        Task delaier = Task.Delay(0);
        
        await foreach (var result in _openAiService.GetStreamingChat(message.Text!, storedUser))
        {
            if (waitMessage is null)
            {
                waitMessage = await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: ".",
                    cancellationToken: cancellationToken);
            }
            
            strBuilder.Append(result.FirstChoice);
            strBuilderBuff.Append(result.FirstChoice);
            
            if (string.IsNullOrWhiteSpace(result.FirstChoice))
                continue;
            
            if (strBuilder.Length < 4096)
            {
                if (delaier.IsCompleted)
                {
                    strBuilderBuff.Clear();
                    delaier = Task.Delay(_streamDelayMilliseconds);
                    await _botClient.EditMessageTextAsync(
                        chatId: message.Chat.Id,
                        messageId: waitMessage.MessageId,
                        text: strBuilder.ToString(),
                        cancellationToken: cancellationToken);
                }
            }
            else
            {
                strBuilder.Clear();
                strBuilder.Append(strBuilderBuff);
                waitMessage = await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: result.FirstChoice,
                    cancellationToken: cancellationToken);
            }
        }

        await _botClient.EditMessageTextAsync(
            chatId: message.Chat.Id,
            messageId: waitMessage.MessageId,
            text: strBuilder.ToString(),
            cancellationToken: cancellationToken);
    }
}