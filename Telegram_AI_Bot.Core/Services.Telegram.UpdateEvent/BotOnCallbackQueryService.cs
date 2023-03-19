using Askmethat.Aspnet.JsonLocalizer.Localizer;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram_AI_Bot.Core.Models;
using Telegram_AI_Bot.Core.Models.Users;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Services.Telegram.OpenAi;
using Telegram_AI_Bot.Core.Telegram;

namespace Telegram_AI_Bot.Core.Services.Telegram.UpdateEvent;

public interface IBotOnCallbackQueryService
{
    Task Handler(CallbackQuery? callbackQuery, CancellationToken cancellationToken);
}

public class BotOnCallbackQueryService : IBotOnCallbackQueryService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<BotOnCallbackQueryService> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJsonStringLocalizer _localizer;

    public BotOnCallbackQueryService(
        ITelegramBotClient botClient,
        ILogger<BotOnCallbackQueryService> logger,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IJsonStringLocalizer localizer)
    {
        _botClient = botClient;
        _logger = logger;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _localizer = localizer;
    }

    public async Task Handler(CallbackQuery? callbackQuery, CancellationToken cancellationToken)
    {
        if (callbackQuery?.Data == null)
            return;
        
        var user = await _userRepository.GetOrCreateIfNotExistsAsync(callbackQuery.From);
        TelegramMessageHelper.SetCulture(user.Language);

        var action = callbackQuery.Data.Split(' ')[0] switch
        {
            var x when x.StartsWith("--") => KeyboardHandle(callbackQuery, cancellationToken),
            // "/help" => Usage(sender, message),
            _ => throw new NotImplementedException()
        };

        await action;

        // await _botClient.AnswerCallbackQueryAsync(
        //     callbackQueryId: callbackQuery.Id,
        //     text: $"Received {callbackQuery.Data}",
        //     cancellationToken: cancellationToken);

        // await _botClient.SendTextMessageAsync(
        //     chatId: callbackQuery.Message!.Chat.Id,
        //     text: $"Received {callbackQuery.Data}",
        //     cancellationToken: cancellationToken);

        await _unitOfWork.CommitAsync();
    }

    private async Task KeyboardHandle(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var args = callbackQuery.Data.Split(' ');
        string command = args[0];
        args = args.Skip(1).ToArray();

        if (!TelegramCommands.Keyboard.All.Contains(command.ToLowerInvariant()))
            return;

        var user = await _userRepository.ByUserIdAsync(callbackQuery.From.Id);

        var action = command switch
        {
            TelegramCommands.Keyboard.MainMenu => KeyboardMainMenu(callbackQuery, args, cancellationToken),
            TelegramCommands.Keyboard.Balance => KeyboardBalance(callbackQuery, args, user, cancellationToken),
            TelegramCommands.Keyboard.Settings => KeyboardSettings(callbackQuery, args, user, cancellationToken),
            TelegramCommands.Keyboard.Settings_SetLanguage => KeyboardLanguage(callbackQuery, args, user,
                cancellationToken),
            TelegramCommands.Keyboard.Help => KeyboardHelp(callbackQuery, args, cancellationToken),
            // TelegramKeyboardCommands.Settings => KeyboardSettings(callbackQuery, user, args),
            // TelegramKeyboardCommands.Settings_SetLanguage => KeyboardSettingsLanguage(callbackQuery, user, args),
            // TelegramKeyboardCommands.Help => KeyboardHelp(callbackQuery, user, args),
        };

        await action;
    }

    private async Task KeyboardMainMenu(CallbackQuery callbackQuery, string[] args, CancellationToken cancellationToken)
    {
        await _botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: _localizer.GetString("MainMenu"),
            replyMarkup: TelegramInlineMenus.MainMenu(_localizer),
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    private async Task KeyboardBalance(CallbackQuery callbackQuery, string[] args, TelegramUser user, CancellationToken cancellationToken)
    {
        await _botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: _localizer.GetString("Balance", user.Balance),
            replyMarkup: TelegramInlineMenus.BalanceMenu(_localizer),
            cancellationToken: cancellationToken);
    }

    private async Task KeyboardSettings(CallbackQuery callbackQuery, string[] args, TelegramUser user, CancellationToken cancellationToken)
    {
        switch (args.FirstOrDefault())
        {
            case "SwitchMode": 
                user.SwitchMode();
                break;
            case "SwitchContext": 
                user.SwitchEnablingContext();
                break;
            case "SwitchStreamingChat":
                user.SwitchEnabledStreamingChat();
                break;
            case "ClearContext":
                if (user.ClearContext())
                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, _localizer.GetString("DeletedContext"),
                        cancellationToken: cancellationToken);
                else
                    return;
                break;
        }

        await _botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: TelegramInlineMenus.GetSettingsText(_localizer, user),
            replyMarkup: TelegramInlineMenus.SettingsMenu(_localizer, user),
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    private async Task KeyboardLanguage(CallbackQuery callbackQuery, string[] args, TelegramUser user,
        CancellationToken cancellationToken)
    {
        if (args.FirstOrDefault() is var lang && !string.IsNullOrEmpty(lang))
        {
            if (lang == user.Language)
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
                return;
            }

            user.SetLanguage(lang);
            TelegramMessageHelper.SetCulture(user.Language);
        }

        await _botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: _localizer.GetString("Language.Title"),
            replyMarkup: TelegramInlineMenus.LanguageMenu(_localizer),
            cancellationToken: cancellationToken);
    }

    private async Task KeyboardHelp(CallbackQuery callbackQuery, string[] args, CancellationToken cancellationToken)
    {
        await _botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: _localizer.GetString("HelpText"),
            replyMarkup: TelegramInlineMenus.HelpMenu(_localizer),
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