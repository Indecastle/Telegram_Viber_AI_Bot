using Askmethat.Aspnet.JsonLocalizer.Localizer;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram_AI_Bot.Core.Models.Users;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Services.Telegram.OpenAi;
using Telegram_AI_Bot.Core.Telegram;

namespace Telegram_AI_Bot.Core.Services.Telegram.UpdateEvent;

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
    private readonly ITelegramOpenAiService _telegramOpenAiService;
    private readonly IJsonStringLocalizer _localizer;

    public BotOnMessageReceivedService(
        ITelegramBotClient botClient,
        ILogger<BotOnMessageReceivedService> logger,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ITelegramOpenAiService telegramOpenAiService, IJsonStringLocalizer localizer)
    {
        _botClient = botClient;
        _logger = logger;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _telegramOpenAiService = telegramOpenAiService;
        _localizer = localizer;
    }

    public async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetOrCreateIfNotExistsAsync(message.From ?? throw new ArgumentNullException());
        TelegramMessageHelper.SetCulture(user.Language);


        LogMessage(message, user);

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
            _ => _telegramOpenAiService.Handler(message, user, cancellationToken)
        };
        
        await action;

        await _unitOfWork.CommitAsync();
    }

    private void LogMessage(Message message, TelegramUser user)
    {
        switch (message.Type)
        {
            case MessageType.Text:
                _logger.LogInformation("Receive message from user: {UserName} | with text: {Text}", message.From!.Username ?? message.From.FirstName, message.Text);
                break;
            default:
                _logger.LogInformation("Receive message type: {Type}, from user: {UserName}", message.Type, message.From!.Username ?? message.From.FirstName);
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
        
        await _botClient.SendChatActionAsync(
            chatId: message.Chat.Id,
            chatAction: ChatAction.Typing,
            cancellationToken: cancellationToken);
        
        var action = command switch
        {
            TelegramCommands.Start => MainMenuCommand(message, args, user, cancellationToken),
            TelegramCommands.MainMenu => MainMenuCommand(message, args, user, cancellationToken),
            TelegramCommands.Settings => SettingsCommand(message, args, user, cancellationToken),
            TelegramCommands.Balance => BalanceCommand(message, args, user, cancellationToken),
            TelegramCommands.Help => HelpCommand(message, args, cancellationToken),
        };

        await action;
    }

    private async Task MainMenuCommand(Message message, string[] args, TelegramUser user, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: _localizer.GetString("MainMenu"),
            replyMarkup: TelegramInlineMenus.MainMenu(_localizer),
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    private async Task SettingsCommand(Message message, string[] args, TelegramUser user, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: TelegramInlineMenus.GetSettingsText(_localizer, user),
            replyMarkup: TelegramInlineMenus.SettingsMenu(_localizer, user),
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }
    
    private async Task BalanceCommand(Message message, string[] args, TelegramUser user, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: _localizer.GetString("Balance", user.Balance),
            replyMarkup: TelegramInlineMenus.BalanceMenu(_localizer),
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }
    
    private async Task HelpCommand(Message message, string[] args, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: _localizer.GetString("HelpText"),
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
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

    
    private async Task<Message> Usage(Message message, string[] args,
        CancellationToken cancellationToken)
    {
        const string usage = "Usage:\n" +
                             "/inline_keyboard - send inline keyboard\n" +
                             "/keyboard    - send custom keyboard\n" +
                             "/remove      - remove custom keyboard\n" +
                             "/photo       - send a photo\n" +
                             "/request     - request location or contact\n" +
                             "/inline_mode - send keyboard with Inline Query";

        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: usage,
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }
}