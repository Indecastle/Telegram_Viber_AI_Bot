using Askmethat.Aspnet.JsonLocalizer.Localizer;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.Payments;
using Telegram_AI_Bot.Core.Services.Telegram.Payments;
using Telegram_AI_Bot.Core.Services.Telegram.UpdateEvent;

namespace Telegram_AI_Bot.Api.Services;

public class UpdateHandler(ITelegramBotClient botClient,
        ILogger<UpdateHandler> logger,
        IBotOnMessageReceivedService botOnMessageReceivedService,
        IJsonStringLocalizer localizer,
        IBotOnCallbackQueryService botOnCallbackQueryService,
        ITelegramPaymentsService paymentsService)
    : IUpdateHandler
{

    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        var handler = update switch
        {
            { ChatJoinRequest: { } chatJoinRequest }                       => throw new NotImplementedException(),
            // { Message: { Type: MessageType.SuccessfulPayment } SuccessPaymentMessage }                       => _botOnMessageReceivedService.BotOnMessageReceived(message, cancellationToken),
            { Message: { Type: MessageType.Photo } message }                       => botOnMessageReceivedService.BotOnPhotoReceived(message, cancellationToken),
            { Message: { } message }                       => botOnMessageReceivedService.BotOnMessageReceived(message, false, cancellationToken),
            { EditedMessage: { } message }                 => botOnMessageReceivedService.BotOnMessageReceived(message, true, cancellationToken),
            { CallbackQuery: { } callbackQuery }           => botOnCallbackQueryService.Handler(callbackQuery, cancellationToken),
            { InlineQuery: { } inlineQuery }               => BotOnInlineQueryReceived(inlineQuery, cancellationToken),
            { ChosenInlineResult: { } chosenInlineResult } => BotOnChosenInlineResultReceived(chosenInlineResult, cancellationToken),
            { PreCheckoutQuery: { } preCheckoutQuery } => BotPreCheckoutHandlerAsync(preCheckoutQuery, cancellationToken),
            { ShippingQuery: { } shippingQuery } => BotShippingHandlerAsync(shippingQuery, cancellationToken),
            _                                              => UnknownUpdateHandlerAsync(update, cancellationToken)
        };

        await handler;  
    }

    private async Task BotShippingHandlerAsync(ShippingQuery shippingQuery, CancellationToken cancellationToken)
    {
        // await _botClient.AnswerShippingQueryAsync(shippingQuery.Id, "errorMessage1");
        await botClient.AnswerShippingQueryAsync(shippingQuery.Id, new []{ 
            new ShippingOption
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Abc1",
            Prices = new []
            {
                new LabeledPrice("Abc1_price1", 5*100),
                new LabeledPrice("Abc1_price2", 7*100)
            }
        },
            new ShippingOption
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Abc2",
                Prices = new []
                {
                    new LabeledPrice("Abc1_price1", 2*100),
                }
            }
        });
    }
    
    private Task<Message> SendInlineKeyboard(ITelegramBotClient arg1, Message arg2, CancellationToken arg3)
    {
        throw new NotImplementedException();
    }

    // Process Inline Keyboard callback data
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);

        await botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            text: $"Received {callbackQuery.Data}",
            cancellationToken: cancellationToken);

        await botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            text: $"Received {callbackQuery.Data}",
            cancellationToken: cancellationToken);
    }

    #region Inline Mode

    private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

        InlineQueryResult[] results = {
            // displayed result
            new InlineQueryResultArticle(
                id: "1",
                title: "TgBots",
                inputMessageContent: new InputTextMessageContent("hello")),
            new InlineQueryResultArticle(
                id: "2",
                title: "TgBots",
                inputMessageContent: new InputTextMessageContent("https://en.wikipedia.org/wiki/German_Shepherd")),
            new InlineQueryResultArticle(
                id: "3",
                title: "Azaz",
                inputMessageContent: new InputContactMessageContent("+37533123456", "MyDad")),
            new InlineQueryResultContact(
                id: "4",
                firstName: "Wooow",
                phoneNumber: "3742232323")
        };

        await botClient.AnswerInlineQueryAsync(
            inlineQueryId: inlineQuery.Id,
            results: results,
            cacheTime: 0,
            isPersonal: true,
            cancellationToken: cancellationToken);
    }

    private async Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);

        await botClient.SendTextMessageAsync(
            chatId: chosenInlineResult.From.Id,
            text: $"You chose result with Id: {chosenInlineResult.ResultId}",
            cancellationToken: cancellationToken);
    }

    #endregion

#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable RCS1163 // Unused parameter.
    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
#pragma warning restore RCS1163 // Unused parameter.
#pragma warning restore IDE0060 // Remove unused parameter
    {
        logger.LogWarning("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);

        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }
    
    private async Task BotPreCheckoutHandlerAsync(PreCheckoutQuery preCheckoutQuery, CancellationToken cancellationToken)
    {
        // await _botClient.AnswerPreCheckoutQueryAsync(preCheckoutQuery.Id, "Error-NoPayment", cancellationToken: cancellationToken);
        await paymentsService.PreCheckoutHandlerAsync(preCheckoutQuery, cancellationToken);
    }
}   