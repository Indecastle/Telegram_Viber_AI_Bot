using Askmethat.Aspnet.JsonLocalizer.Localizer;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Payments;
using Telegram_AI_Bot.Core.Models.Users;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Services.OpenAi;
using Telegram_AI_Bot.Core.Telegram;

namespace Telegram_AI_Bot.Core.Services.Telegram.Payments;

public interface ITelegramPaymentsService
{
    Task Handler(long chatId, long messageId, string[] args, TelegramUser user, CancellationToken cancellationToken);
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

    public TelegramPaymentsService(
        ITelegramBotClient botClient,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IJsonStringLocalizer localizer,
        IOptions<PaymentsConfiguration> paymentsOptions,
        IOptions<OpenAiConfiguration> openAiOptions)
    {
        _botClient = botClient;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _localizer = localizer;
        _paymentsOptions = paymentsOptions.Value;
        _openAiOptions = openAiOptions.Value;
    }

    public async Task Handler(long chatId, long messageId, string[] args, TelegramUser user, CancellationToken cancellationToken)
    {
        var token = args[0] switch
        {
            "Stripe" => _paymentsOptions.StripeProviderToken,
            _ => throw new ArgumentException()
        };
        
        if (!int.TryParse(args[1], out var amount) || !_paymentsOptions.Choices!.Contains(amount))
            throw new ArgumentException();

        await _botClient.SendInvoiceAsync(
            chatId,
            "Pay tokens",
            _localizer.GetString("PaymentDescription", _openAiOptions.CalculateTokens(amount)),
            "Payload1",
            token,
            "USD",
            new []{ new LabeledPrice("LabeledPrice1", amount) },
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
        var user = await _userRepository.GetOrCreateIfNotExistsAsync(preCheckoutQuery.From ?? throw new ArgumentNullException());
        TelegramMessageHelper.SetCulture(user.Language);
        
        long amount = _openAiOptions.CalculateTokens(preCheckoutQuery.TotalAmount);

        user.IncreaseBalance(amount);

        await _botClient.AnswerPreCheckoutQueryAsync(preCheckoutQuery.Id, cancellationToken: cancellationToken);
        
        await _unitOfWork.CommitAsync();
    }
}