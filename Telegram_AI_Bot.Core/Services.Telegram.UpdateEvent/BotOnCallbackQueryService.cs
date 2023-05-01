using Askmethat.Aspnet.JsonLocalizer.Localizer;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram_AI_Bot.Core.Models;
using Telegram_AI_Bot.Core.Models.Users;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Services.Telegram.Payments;
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
    private readonly ITelegramPaymentsService _paymentsService;
    private readonly PaymentsConfiguration _paymentsOptions;
    private readonly IExchangeRates _rates;

    public BotOnCallbackQueryService(
        ITelegramBotClient botClient,
        ILogger<BotOnCallbackQueryService> logger,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IJsonStringLocalizer localizer,
        ITelegramPaymentsService paymentsService,
        IOptions<PaymentsConfiguration> paymentsOptions,
        IExchangeRates rates)
    {
        _botClient = botClient;
        _logger = logger;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _localizer = localizer;
        _paymentsService = paymentsService;
        _rates = rates;
        _paymentsOptions = paymentsOptions.Value;
    }

    public async Task Handler(CallbackQuery? callbackQuery, CancellationToken cancellationToken)
    {
        if (callbackQuery?.Data == null)
            return;

        var user = await _userRepository.GetOrCreateIfNotExistsAsync(callbackQuery.From);
        TelegramMessageHelper.SetCulture(user.Language);
        
        // if (user.IsTyping)
        //     return;

        LogCallbackQuery(callbackQuery, user);

        var action = callbackQuery.Data.Split(' ')[0] switch
        {
            var x when x.StartsWith("--") => KeyboardHandle(callbackQuery, cancellationToken),
            // "/help" => Usage(sender, message),
            _ => throw new NotImplementedException()
        };

        await action;
    }

    private async Task KeyboardHandle(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var args = callbackQuery.Data.Split(' ');
        string command = args[0];
        args = args.Skip(1).ToArray();

        if (!TelegramCommands.Keyboard.All.Contains(command))
            return;

        var user = await _userRepository.ByUserIdAsync(callbackQuery.From.Id);

        var action = command switch
        {
            TelegramCommands.Keyboard.MainMenu => KeyboardMainMenu(callbackQuery, args, cancellationToken),
            TelegramCommands.Keyboard.Balance => KeyboardBalance(callbackQuery, args, user, cancellationToken),
            TelegramCommands.Keyboard.Payments => KeyboardPayments(callbackQuery, args, user, cancellationToken),
            TelegramCommands.Keyboard.Settings => KeyboardSettings(callbackQuery, args, user, cancellationToken),
            TelegramCommands.Keyboard.Settings_SystemMessage => KeyboardSettingsSystemMessage(callbackQuery, args, user, cancellationToken),
            TelegramCommands.Keyboard.Settings_SetChatModel => KeyboardSettingsChatModel(callbackQuery, args, user,
                cancellationToken),
            TelegramCommands.Keyboard.Settings_SetLanguage => KeyboardLanguage(callbackQuery, args, user,
                cancellationToken),
            TelegramCommands.Keyboard.Help => KeyboardHelp(callbackQuery, args, cancellationToken),
        };

        await action;
    }

    private async Task KeyboardSettingsSystemMessage(CallbackQuery callbackQuery, string[] args, TelegramUser user, CancellationToken cancellationToken)
    {
        if (args.FirstOrDefault() is { } command)
        {
            if (command == "Reset")
            {
                user.ResetSystemMessage();
                await _botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: _localizer.GetString("SystemMessageMenu.ResetTitle"),
                    replyMarkup: TelegramInlineMenus.BackPrevMenu(_localizer,
                        TelegramCommands.Keyboard.Settings_SystemMessage),
                    cancellationToken: cancellationToken);
            }
            if (command == "Change")
            {
                user.SetWaitState(WaitState.SystemMessage);
                await _botClient.SendTextMessageAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    text: _localizer.GetString("SystemMessageMenu.ChangeSystemMessage"),
                    cancellationToken: cancellationToken);
            }
            
            await _unitOfWork.CommitAsync();
            return;
        }

        await _botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: user.SystemMessage != null
                ? _localizer.GetString("SystemMessageMenu.Title", user.SystemMessage)
                : _localizer.GetString("SystemMessageMenu.TitleDefault"),
            replyMarkup: TelegramInlineMenus.SystemMessageMenu(_localizer, user),
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    private async Task KeyboardPayments(CallbackQuery callbackQuery, string[] args, TelegramUser user, CancellationToken cancellationToken)
    {
        if (args.Length == 1)
        {
            if (!_rates.Rate_Ton_Usd.HasValue)
                return;

            await _botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: _localizer.GetString("TonCoin.ChooseCurrency"),
                replyMarkup: TelegramInlineMenus.Payments(_localizer, user, _paymentsOptions, _rates, int.Parse(args[0])),
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
            return;
        }
        
        if (args.Length == 3)
        {
            await _paymentsService.Handler(callbackQuery.From.Id, callbackQuery.Message!.MessageId, args, user,
                cancellationToken);
            return;
        }

        await _botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: TelegramInlineMenus.GetPaymentsText(_localizer, user, _paymentsOptions),
            replyMarkup: TelegramInlineMenus.PaymentChoices(_localizer, _paymentsOptions),
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    private async Task KeyboardSettingsChatModel(CallbackQuery callbackQuery, string[] args, TelegramUser user,
        CancellationToken cancellationToken)
    {
        if (args.FirstOrDefault() is { } arg && ChatModel.All.Contains(arg))
        {
            if (arg == user.ChatModel)
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
                return;
            }
            
            user.SetChatModel(arg!);

            if (args.Length == 2 && args[1] == "Begin")
            {
                if (arg == ChatModel.Gpt35)
                {
                    user.SwitchEnablingContext(true);
                    await _unitOfWork.CommitAsync();
                }
                
                await _botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    _localizer.GetString("YouChoseChatModelAlert_" + user.ChatModel!.Value),
                    showAlert: true,
                    cancellationToken: cancellationToken);

                await KeyboardMainMenu(callbackQuery, args, cancellationToken);
                return;
            }
            
            await _unitOfWork.CommitAsync();
        }

        await _botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: _localizer.GetString("ChooseChatModel"),
            replyMarkup: TelegramInlineMenus.SetChatModel(_localizer, user),
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    private async Task KeyboardMainMenu(CallbackQuery callbackQuery, string[] args, CancellationToken cancellationToken)
    {
        await _botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: _localizer.GetString("MainMenu"),
            replyMarkup: TelegramInlineMenus.MainMenu(_localizer),
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    private async Task KeyboardBalance(CallbackQuery callbackQuery, string[] args, TelegramUser user,
        CancellationToken cancellationToken)
    {
        await _botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: _localizer.GetString("Balance", user.Balance.ToString("N0")),
            replyMarkup: TelegramInlineMenus.BalanceMenu(_localizer),
            cancellationToken: cancellationToken);
    }

    private async Task KeyboardSettings(CallbackQuery callbackQuery, string[] args, TelegramUser user,
        CancellationToken cancellationToken)
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
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: TelegramInlineMenus.GetSettingsText(_localizer, user),
            replyMarkup: TelegramInlineMenus.SettingsMenu(_localizer, user),
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);

        await _unitOfWork.CommitAsync();
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

        await _unitOfWork.CommitAsync();
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

    private void LogCallbackQuery(CallbackQuery callbackQuery, TelegramUser user)
    {
        _logger.LogInformation("Receive callback from user: {UserName} | with data: {Data}", user.Username ?? user.Name.FullName(), callbackQuery.Data);
    }
}