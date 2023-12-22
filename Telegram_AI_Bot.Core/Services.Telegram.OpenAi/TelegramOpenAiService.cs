using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Askmethat.Aspnet.JsonLocalizer.Localizer;
using CryptoPay.Exceptions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using MoreLinq.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram_AI_Bot.Core.Models;
using Telegram_AI_Bot.Core.Models.Users;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Services.OpenAi;
using Telegram_AI_Bot.Core.Services.OpenAi.Tools;
using Telegram_AI_Bot.Core.Telegram;

namespace Telegram_AI_Bot.Core.Services.Telegram.OpenAi;

public interface ITelegramOpenAiService
{
    Task MessageHandler(Message? message, TelegramUser user, CancellationToken cancellationToken);
    Task PhotoHandler(Message? message, TelegramUser user, CancellationToken cancellationToken);
}

public class TelegramOpenAiService(ITelegramBotClient botClient,
        IUnitOfWork unitOfWork,
        IOpenAiService openAiService,
        IJsonStringLocalizer localizer,
        IOpenAiAllMessageRepository allMessageRepository,
        IOptions<OpenAiConfiguration> openAiOptions)
    : ITelegramOpenAiService
{
    private readonly OpenAiConfiguration _openAiOptions = openAiOptions.Value;

    private static readonly int _streamDelayMilliseconds = 3000;

    public async Task MessageHandler(Message? message, TelegramUser user, CancellationToken cancellationToken)
    {
        await botClient.SendChatActionAsync(
            chatId: message.Chat.Id,
            chatAction: ChatAction.Typing,
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(user.ChatModel))
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: localizer.GetString("ChooseChatModelBegin"),
                replyMarkup: TelegramInlineMenus.SetChatModelBegin(localizer, user),
                cancellationToken: cancellationToken);
            return;
        }
        
        if (!user.IsPositiveBalance())
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: localizer.GetString("NoBalance"),
                replyMarkup: TelegramInlineMenus.BalanceMenu(localizer, false),
                cancellationToken: cancellationToken);
            return;
        }

        if (user.ReduceContextIfNeed(message.Text, openAiService))
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: localizer.GetString("ReducedContext"),
                cancellationToken: cancellationToken);
        }

        // using var scopeTr = new TransactionScope(
        //     TransactionScopeOption.RequiresNew,
        //     new TransactionOptions { IsolationLevel = IsolationLevel.RepeatableRead },
        //     TransactionScopeAsyncFlowOption.Enabled);
        
        if (user.SelectedMode == SelectedMode.Chat)
        {
            try
            {
                user.SetTyping(true);
                await unitOfWork.CommitAsync();
                
                if (user.IsEnabledStreamingChat())
                    await SendGradually(message, user, cancellationToken);
                else
                    await SendImmediately(message, user, cancellationToken);
            }
            catch (NoEnoughBalance e)
            {
                if (e.IsOnlyOverRequest)
                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: localizer.GetString("NoBalance"),
                        replyMarkup: TelegramInlineMenus.BalanceMenu(localizer, false),
                        cancellationToken: cancellationToken);
            }
            finally
            {
                user.SetTyping(false);
                await unitOfWork.CommitAsync();
            }
            
        }
        else
        {
            var url = await openAiService.ImageHandler(message.Text, user);

            await botClient.SendPhotoAsync(
                chatId: message.Chat.Id,
                photo: new InputFileUrl(url),
                cancellationToken: cancellationToken);
        }
    }

    public async Task PhotoHandler(Message? message, TelegramUser user, CancellationToken cancellationToken)
    {
        var file = await botClient.GetFileAsync(message.Photo[^1].FileId, cancellationToken: cancellationToken);
        using var stream = new MemoryStream((int)file.FileSize);
        await botClient.DownloadFileAsync(file.FilePath, stream, cancellationToken);

        await ResizeImage(stream, cancellationToken);
        var tool = new TelegramOpenAiGenerationImageTool(botClient, _openAiOptions);
        string photoLink = await tool.UploadImageToImgure(stream, cancellationToken);
        
        user.AddPhoto(photoLink, DateTimeOffset.UtcNow);
        await allMessageRepository.AddAsync(new OpenAiAllMessage(Guid.NewGuid(), user.Id, user.UserId, photoLink, MessageType.Photo, true, DateTimeOffset.UtcNow));
        await unitOfWork.CommitAsync();
        
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: localizer.GetString("UploadPhotoSuccess"),
            // replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }
    
    private async Task ResizeImage(MemoryStream stream, CancellationToken cancellationToken)
    {
        stream.Position = 0;
        using var image = await Image.LoadAsync(stream, cancellationToken);
        image.Mutate(x => x.Resize(512, 512));
        stream.SetLength(0);
        await image.SaveAsJpegAsync(stream, cancellationToken);
        stream.Position = 0;
    }
    
    

    private async Task SendImmediately(Message message, TelegramUser storedUser, CancellationToken cancellationToken)
    {
        var waitMessage = await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: localizer.GetString("Wait"),
            // replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
        
        var result = await openAiService.ChatHandler(message.Text, storedUser, cancellationToken);
        var textResult = result.FirstChoice.Message.ToString()!.Trim();
        
        if (result.FirstChoice.Message.ToolCalls != null)
        {
            var tool = new TelegramOpenAiGenerationImageTool(botClient, _openAiOptions);
            var prompt = tool.GetPromptFromJson(result.FirstChoice.Message.ToolCalls[0].Function.Arguments.ToString());
            var photoLink = await tool.GenerateAndSendPhotoAsync(message, prompt, cancellationToken);
            await botClient.DeleteMessageAsync(waitMessage.Chat.Id, waitMessage.MessageId, cancellationToken: cancellationToken);
            await SaveMessage(storedUser, message.Text, photoLink);
        }
        else
        {
            if (string.IsNullOrEmpty(textResult))
                await botClient.EditMessageTextAsync(
                    chatId: message.Chat.Id,
                    messageId: waitMessage.MessageId,
                    text: "bad request",
                    cancellationToken: cancellationToken);
        
            await SendTextMessage(message, waitMessage.MessageId, textResult, cancellationToken);
            await SaveMessage(storedUser, message.Text, textResult);
        }
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
                await botClient.EditMessageTextAsync(
                    chatId: message.Chat.Id,
                    messageId: waitMessageId,
                    text: text,
                    cancellationToken: cancellationToken);
            else
                await botClient.SendTextMessageAsync(
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
        var strBuilderTotal = new StringBuilder();
        var strBuilder = new StringBuilder();
        var strBuilderBuff = new StringBuilder();
        Task delaier = Task.Delay(_streamDelayMilliseconds);

        try
        {
            (string Name, StringBuilder strBuilder)[] toolsResult = {};
            
            await foreach (var result in openAiService.GetStreamingChat(message.Text!, storedUser, cancellationToken))
            {
                if (waitMessage is null)
                {
                    waitMessage = await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "...",
                        cancellationToken: cancellationToken);
                }
                
                if (result.FirstChoice.Delta.ToolCalls != null)
                {
                    if (toolsResult.Length == 0)
                    {
                        toolsResult = result.FirstChoice.Delta.ToolCalls
                            .Select(x => (x.Function.Name, new StringBuilder())).ToArray();
                    }

                    for (int i = 0; i < result.FirstChoice.Delta.ToolCalls.Count; i++)
                    {
                        toolsResult[i].strBuilder.Append(result.FirstChoice.Delta.ToolCalls[i].Function.Arguments);
                    }
                    
                    continue;
                }
            
                strBuilder.Append(result.FirstChoice);
                strBuilderTotal.Append(result.FirstChoice);
                strBuilderBuff.Append(result.FirstChoice);
            
                if (string.IsNullOrWhiteSpace(result.FirstChoice))
                    continue;

                if (strBuilder.Length < 4096)
                {
                    if (delaier.IsCompleted)
                    {
                        strBuilderBuff.Clear();
                        delaier = Task.Delay(_streamDelayMilliseconds);
                        await EditCurrentMessage(message.Chat.Id, waitMessage, strBuilder.ToString(), true,
                            cancellationToken);
                    }
                }
                else
                {
                    strBuilder.Clear();
                    strBuilder.Append(strBuilderBuff);
                    waitMessage = await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: result.FirstChoice,
                        cancellationToken: cancellationToken);
                }
            }

            if (toolsResult.Length != 0)
            {
                var tool = new TelegramOpenAiGenerationImageTool(botClient, _openAiOptions);
                var prompt = tool.GetPromptFromJson(toolsResult[0].strBuilder.ToString());
                var photoLink = await tool.GenerateAndSendPhotoAsync(message, prompt, cancellationToken);
                await botClient.DeleteMessageAsync(waitMessage.Chat.Id, waitMessage.MessageId);
                await SaveMessage(storedUser, message.Text, photoLink);
            }
            else
            {
                await EditCurrentMessage(message.Chat.Id, waitMessage, strBuilder.ToString(), strBuilderBuff.Length > 0, cancellationToken);
                await SaveMessage(storedUser, message.Text, strBuilderTotal.ToString());
            }
        }
        catch (NoEnoughBalance e)
        {
            if (!e.IsOnlyOverRequest)
            {
                await SaveMessage(storedUser, message.Text, strBuilderTotal.ToString());
                await EditCurrentMessage(message.Chat.Id, waitMessage, strBuilder.ToString(), strBuilderBuff.Length > 0, cancellationToken);
            }
            throw;
        }
    }
    
    private async Task SaveMessage(TelegramUser user, string messageText, string textResult)
    {
        await allMessageRepository.AddRangeAsync(new[]
        {
            new OpenAiAllMessage(Guid.NewGuid(), user.Id, user.UserId, messageText, MessageType.Text, true, DateTimeOffset.UtcNow),
            new OpenAiAllMessage(Guid.NewGuid(), user.Id, user.UserId, textResult, MessageType.Text, false, DateTimeOffset.UtcNow),
        });
    }

    private async Task EditCurrentMessage(long chatId, Message waitMessage, string responseText, bool canEdit, CancellationToken cancellationToken)
    {
        if (!canEdit) return;
        
        await botClient.EditMessageTextAsync(
            chatId: chatId,
            messageId: waitMessage.MessageId,
            text: responseText,
            cancellationToken: cancellationToken);
    }
}