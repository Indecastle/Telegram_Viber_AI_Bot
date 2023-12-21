using System.Net.Http.Headers;
using System.Text;
using Askmethat.Aspnet.JsonLocalizer.Localizer;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using MoreLinq.Extensions;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram_AI_Bot.Core.Models;
using Telegram_AI_Bot.Core.Models.Users;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Services.OpenAi;
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
        string photoLink = await UploadImageToImgure(stream, cancellationToken);
        
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
    
    private async Task<string> UploadImageToImgure(MemoryStream stream, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Client-ID", _openAiOptions.ImgurToken);

        var content = new ByteArrayContent(stream.ToArray());

        var response = await client.PostAsync("https://api.imgur.com/3/image", content);
        var responseResult = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<dynamic>(responseResult);
        return result!.data.link;
    }

    private async Task SendImmediately(Message message, TelegramUser storedUser, CancellationToken cancellationToken)
    {
        var waitMessage = await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: localizer.GetString("Wait"),
            // replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
        
        var textResult = await openAiService.ChatHandler(message.Text, storedUser, cancellationToken);

        if (string.IsNullOrEmpty(textResult))
            await botClient.EditMessageTextAsync(
                chatId: message.Chat.Id,
                messageId: waitMessage.MessageId,
                text: "bad request",
                cancellationToken: cancellationToken);
        else
            await unitOfWork.CommitAsync();
        
        await SendTextMessage(message, waitMessage.MessageId, textResult, cancellationToken);
        await SaveMessage(storedUser, message.Text, textResult);
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
            await foreach (var result in openAiService.GetStreamingChat(message.Text!, storedUser, cancellationToken))
            {
                if (waitMessage is null)
                {
                    waitMessage = await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "...",
                        cancellationToken: cancellationToken);
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
            
            await EditCurrentMessage(message.Chat.Id, waitMessage, strBuilder.ToString(), strBuilderBuff.Length > 0, cancellationToken);
            await SaveMessage(storedUser, message.Text, strBuilderTotal.ToString());
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