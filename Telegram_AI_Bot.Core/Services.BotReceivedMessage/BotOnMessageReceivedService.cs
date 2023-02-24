using Microsoft.Extensions.Logging;
using MyTemplate.App.Core.Ports.DataAccess;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram_AI_Bot.Core.Ports.DataAccess;

namespace Telegram_AI_Bot.Core.Services.BotReceivedMessage;

public interface IBotOnMessageReceivedService
{
    Task BotOnMessageReceived(Message message, CancellationToken cancellationToken);
}

public class BotOnMessageReceivedService : IBotOnMessageReceivedService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<BotOnMessageReceivedService> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BotOnMessageReceivedService(
        ITelegramBotClient botClient,
        ILogger<BotOnMessageReceivedService> logger,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _botClient = botClient;
        _logger = logger;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Receive message type: {MessageType}", message.Type);
        if (message.Text is not { } messageText)
            return;
        Func<ITelegramBotClient, Message, CancellationToken, Task<Message>> a = SendInlineKeyboard;
        var action = messageText.Split(' ')[0] switch
        {
            "/inline_keyboard" => SendInlineKeyboard(_botClient, message, cancellationToken),
            "/keyboard" => SendReplyKeyboard(_botClient, message, cancellationToken),
            "/remove" => RemoveKeyboard(_botClient, message, cancellationToken),
            "/photo" => SendFile(_botClient, message, cancellationToken),
            "/request" => RequestContactAndLocation(_botClient, message, cancellationToken),
            "/inline_mode" => StartInlineQuery(_botClient, message, cancellationToken),
            "/throw" => FailingHandler(_botClient, message, cancellationToken),
            "/help" => Usage(_botClient, message, cancellationToken),
            _ => Usage(_botClient, message, cancellationToken)
        };
        
        Message sentMessage = await action;
        _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);
    }

    // Send inline keyboard
    // You can process responses in BotOnCallbackQueryReceived handler
    private async Task<Message> SendInlineKeyboard(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        await botClient.SendChatActionAsync(
            chatId: message.Chat.Id,
            chatAction: ChatAction.Typing,
            cancellationToken: cancellationToken);

        // Simulate longer running task
        await Task.Delay(500, cancellationToken);

        InlineKeyboardMarkup inlineKeyboard = new(
            new[]
            {
                // first row
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("1.1", "11"),
                    InlineKeyboardButton.WithCallbackData("1.2", "12"),
                },
                // second row
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("2.1", "21"),
                    InlineKeyboardButton.WithCallbackData("2.2", "22"),
                },
            });

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Choose",
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken);
    }

    private async Task<Message> SendReplyKeyboard(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        ReplyKeyboardMarkup replyKeyboardMarkup = new(
            new[]
            {
                new KeyboardButton[] { "1.1", "1.2" },
                new KeyboardButton[] { "2.1", "2.2" },
            })
        {
            ResizeKeyboard = true
        };

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Choose",
            replyMarkup: replyKeyboardMarkup,
            cancellationToken: cancellationToken);
    }

    private async Task<Message> RemoveKeyboard(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Removing keyboard",
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }

    private async Task<Message> SendFile(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        await botClient.SendChatActionAsync(
            message.Chat.Id,
            ChatAction.UploadPhoto,
            cancellationToken: cancellationToken);

        const string filePath = "Files/tux.png";
        await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();

        return await botClient.SendPhotoAsync(
            chatId: message.Chat.Id,
            photo: new InputFile(fileStream, fileName),
            caption: "Nice Picture",
            cancellationToken: cancellationToken);
    }

    private async Task<Message> RequestContactAndLocation(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        ReplyKeyboardMarkup RequestReplyKeyboard = new(
            new[]
            {
                KeyboardButton.WithRequestLocation("Location"),
                KeyboardButton.WithRequestContact("Contact"),
            });

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Who or Where are you?",
            replyMarkup: RequestReplyKeyboard,
            cancellationToken: cancellationToken);
    }

    private async Task<Message> StartInlineQuery(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        InlineKeyboardMarkup inlineKeyboard = new(
            InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Inline Mode"));

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Press the button to start Inline Query",
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken);
    }
    
    private Task<Message> FailingHandler(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        throw new IndexOutOfRangeException();
    }

    
    private async Task<Message> Usage(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        const string usage = "Usage:\n" +
                             "/inline_keyboard - send inline keyboard\n" +
                             "/keyboard    - send custom keyboard\n" +
                             "/remove      - remove custom keyboard\n" +
                             "/photo       - send a photo\n" +
                             "/request     - request location or contact\n" +
                             "/inline_mode - send keyboard with Inline Query";

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: usage,
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }
}