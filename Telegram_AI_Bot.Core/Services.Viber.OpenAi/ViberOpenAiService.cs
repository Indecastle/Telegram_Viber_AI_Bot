using System.Transactions;
using Askmethat.Aspnet.JsonLocalizer.Localizer;
using Microsoft.Extensions.Localization;
using Telegram_AI_Bot.Core.Models;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Ports.DataAccess.Viber;
using Telegram_AI_Bot.Core.Services.OpenAi;
using Telegram_AI_Bot.Core.Viber;
using Viber.Bot.NetCore.Models;
using Viber.Bot.NetCore.RestApi;
using InternalViberUser = Viber.Bot.NetCore.Models.ViberUser.User;

namespace Telegram_AI_Bot.Core.Services.Viber.OpenAi;

public interface IViberOpenAiService
{
    Task Handler(InternalViberUser sender, ViberMessage.TextMessage message);
}

public class ViberOpenAiService : IViberOpenAiService
{
    private readonly IViberBotApi _botClient;
    private readonly IViberUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOpenAiService _openAiService;
    private readonly IJsonStringLocalizer _localizer;

    public ViberOpenAiService(
        IViberBotApi botClient,
        IViberUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IOpenAiService openAiService,
        IJsonStringLocalizer localizer)
    {
        _botClient = botClient;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _openAiService = openAiService;
        _localizer = localizer;
    }

    public async Task Handler(InternalViberUser sender, ViberMessage.TextMessage message)
    {
        var newMessage = ViberMessageHelper.GetKeyboardMainMenuMessage(_localizer, sender, "");

        var storedUser = await _userRepository.ByUserIdAsync(sender.Id);
        if (!storedUser.IsPositiveBalance())
        {
            newMessage.Text = _localizer.GetString("NoBalance");
            await _botClient.SendMessageV6Async(newMessage);
            return;
        }

        using var transactionScope = new TransactionScope(
            TransactionScopeOption.RequiresNew,
            new TransactionOptions { IsolationLevel = IsolationLevel.RepeatableRead },
            TransactionScopeAsyncFlowOption.Enabled);
        
        if (storedUser.SelectedMode == SelectedMode.Chat)
        {
            var textResult = await _openAiService.ChatHandler(message.Text, storedUser, cancellationToken: CancellationToken.None);

            if (!string.IsNullOrEmpty(textResult))
                await _unitOfWork.CommitAsync();

            newMessage.Text = string.IsNullOrEmpty(textResult) ? "bad request" : textResult;

            await _botClient.SendMessageV6Async(newMessage);
        }
        else
        {
            var url = await _openAiService.ImageHandler(message.Text, storedUser);

            await _botClient.SendMessageV6Async(new ViberPictureMessageV6
            {
                Receiver = sender.Id,
                Sender = new InternalViberUser
                {
                    Name = "Chat bot",
                },
                Text = message.Text,
                Media = url,
                // Thumbnail = "https://i.imgur.com/4Qe65rF.png",
                Keyboard = newMessage.Keyboard
            });
        }
        
        transactionScope.Complete();
    }
}