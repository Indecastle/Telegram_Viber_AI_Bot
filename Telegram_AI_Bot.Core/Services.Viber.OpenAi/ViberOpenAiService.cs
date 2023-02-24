using Telegram.Bot.Types;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Ports.DataAccess.Viber;
using Telegram_AI_Bot.Core.Services.OpenAi;
using Telegram_AI_Bot.Core.Services.Viber.TextReceivedService;
using Telegram_AI_Bot.Core.Viber;
using Viber.Bot.NetCore.Models;
using Viber.Bot.NetCore.RestApi;
using InternalViberUser = Viber.Bot.NetCore.Models.ViberUser.User;

namespace Telegram_AI_Bot.Core.Services.Viber.OpenAi;

public interface IViberOpenAiService
{
    Task Handler(InternalViberUser sender, ViberMessage.TextMessage message);
    Task<string?> GetResult(InternalViberUser sender, ViberMessage.TextMessage message);
}

public class ViberOpenAiService : IViberOpenAiService
{
    private readonly IViberKeyboardService _keyboardService;
    private readonly IViberBotApi _botClient;
    private readonly IViberUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOpenAiService _openAiService;

    public ViberOpenAiService(
        IViberKeyboardService keyboardService,
        IViberBotApi botClient,
        IViberUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IOpenAiService openAiService)
    {
        _keyboardService = keyboardService;
        _botClient = botClient;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _openAiService = openAiService;
    }

    public async Task Handler(InternalViberUser sender, ViberMessage.TextMessage message)
    {
        var newMessage = ViberMessageHelper.GetKeyboardMainMenuMessage(sender);
        
        var storedUser = await _userRepository.ByUserIdAsync(sender.Id);
        if (!storedUser.TryDecrementBalance())
        {
            newMessage.Text = "Не хватает средств. \n пополните баланс";
            await _botClient.SendMessageAsync<ViberResponse.SendMessageResponse>(newMessage);
        }

        var textResult = await GetResult(sender, message);

        if (!string.IsNullOrEmpty(textResult))
            await _unitOfWork.CommitAsync();
        
        newMessage.Text = string.IsNullOrEmpty(textResult) ? "bad request" : textResult;

        await _botClient.SendMessageAsync<ViberResponse.SendMessageResponse>(newMessage);
    }

    public async Task<string?> GetResult(InternalViberUser sender, ViberMessage.TextMessage message)
    {
        var result = await _openAiService.Handler(message.Text);
        return "Test Text";
    }
}