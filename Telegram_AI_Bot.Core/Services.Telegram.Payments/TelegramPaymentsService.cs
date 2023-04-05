using Askmethat.Aspnet.JsonLocalizer.Localizer;
using CryptoPay;
using CryptoPay.Types;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Payments;
using Telegram_AI_Bot.Core.Common;
using Telegram_AI_Bot.Core.Models.Users;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Services.OpenAi;
using Telegram_AI_Bot.Core.Telegram;

namespace Telegram_AI_Bot.Core.Services.Telegram.Payments;

public interface ITelegramPaymentsService
{
    Task Handler(long chatId, int messageId, string[] args, TelegramUser user, CancellationToken cancellationToken);
    Task PreCheckoutHandlerAsync(PreCheckoutQuery preCheckoutQuery, CancellationToken cancellationToken);
}

public class TelegramPaymentsService : ITelegramPaymentsService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJsonStringLocalizer _localizer;
    private readonly PaymentsConfiguration _paymentsOptions;
    private readonly OpenAiConfiguration _openAiOptions;
    private readonly CommonConfiguration _commonConfiguration;
    private readonly CryptoPayClient _cryptoTonClient;
    private readonly IExchangeRates _exchangeRates;

    public TelegramPaymentsService(
        ITelegramBotClient botClient,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IJsonStringLocalizer localizer,
        IOptions<PaymentsConfiguration> paymentsOptions,
        IOptions<OpenAiConfiguration> openAiOptions,
        IOptions<CommonConfiguration> commonConfiguration,
        IExchangeRates exchangeRates)
    {
        _botClient = botClient;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _localizer = localizer;
        _exchangeRates = exchangeRates;
        _commonConfiguration = commonConfiguration.Value;
        _paymentsOptions = paymentsOptions.Value;
        _openAiOptions = openAiOptions.Value;
        _cryptoTonClient = new(_paymentsOptions.TonProviderToken!);
    }

    public async Task Handler(long chatId, int messageId, string[] args, TelegramUser user,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(args[2], out var index))
            throw new ArgumentException();

        var amount = _paymentsOptions.TonPriceTuples[index].Rub;
        var tokens = _paymentsOptions.TonPriceTuples[index].Token;

        var rateFrom = Enum.Parse<Assets>(args[0]);
        var rateTo = Enum.Parse<Assets>(args[1]);
        
        var cryptoAmount = _exchangeRates.GetPrice(rateFrom, rateTo, amount);

        await CryptoHandler(chatId, messageId, tokens, rateTo, cryptoAmount.Value, user, cancellationToken);
    }

    private async Task CryptoHandler(long chatId, int messageId, long tokens, Assets rate, decimal amount, TelegramUser user,
        CancellationToken cancellationToken)
    {
        var response = await _cryptoTonClient.CreateInvoiceAsync(
            rate,
            (double)amount,
            payload: $"{user.UserId},{tokens}",
            description: _localizer.GetString("TonCoin.InvoiceDescription"),
            paidBtnName: PaidButtonNames.openBot,
            paidBtnUrl: _commonConfiguration.BotUrl,
            allowAnonymous: false,
            allowComments: false,
            cancellationToken: cancellationToken);

        var payUrl = response.PayUrl;

        await _botClient.EditMessageTextAsync(
            chatId: chatId,
            messageId: messageId,
            text: _localizer.GetString("TonCoin.PressToPayBody", _exchangeRates.Round(amount, 6), rate.ToString()),
            replyMarkup: TelegramInlineMenus.PaymentPressToPay(_localizer, payUrl),
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }
    
    private async Task CardHandler(long chatId, long messageId, string token, int amount, TelegramUser user,
        CancellationToken cancellationToken)
    {
        await _botClient.SendInvoiceAsync(
            chatId,
            "Pay tokens",
            _localizer.GetString("PaymentDescription", _openAiOptions.CalculateTokens(amount)),
            "Payload1",
            token,
            "USD",
            new[] { new LabeledPrice("LabeledPrice1", amount) },
            photoUrl: "https://i.imgur.com/kABxZg1.jpeg",
            photoSize: 200,
            photoHeight: 200,
            photoWidth: 200,
            isFlexible: false,
            // needShippingAddress: false,
            startParameter: "one-month-subscription",
            cancellationToken: cancellationToken);
    }

    public async Task PreCheckoutHandlerAsync(PreCheckoutQuery preCheckoutQuery, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetOrCreateIfNotExistsAsync(preCheckoutQuery.From ??
                                                                     throw new ArgumentNullException());
        TelegramMessageHelper.SetCulture(user.Language);

        long amount = _openAiOptions.CalculateTokens(preCheckoutQuery.TotalAmount);

        user.IncreaseBalance(amount);

        await _botClient.AnswerPreCheckoutQueryAsync(preCheckoutQuery.Id, cancellationToken: cancellationToken);

        await _unitOfWork.CommitAsync();
    }
}