using Microsoft.Extensions.Logging;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Ports.DataAccess.Viber;
using Telegram_AI_Bot.Core.Services.Viber.OpenAi;
using Viber.Bot.NetCore.Models;
using Viber.Bot.NetCore.RestApi;
using static Viber.Bot.NetCore.Models.ViberMessage;

namespace Telegram_AI_Bot.Core.Services.Viber.TextReceivedService;

public interface IViberTextReceivedService
{
    Task Handle(ViberCallbackData update);
}

public class ViberTextReceivedService : IViberTextReceivedService
{
    private readonly IViberBotApi _botClient;
    private readonly ILogger<ViberTextReceivedService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IViberKeyboardService _keyboardService;
    private readonly IViberOpenAiService _viberOpenAiService;

    public ViberTextReceivedService(
        IViberBotApi botClient,
        ILogger<ViberTextReceivedService> logger,
        IUnitOfWork unitOfWork,
        IViberKeyboardService keyboardService,
        IViberOpenAiService viberOpenAiService)
    {
        _botClient = botClient;
        _logger = logger;
        _unitOfWork = unitOfWork;
        _keyboardService = keyboardService;
        _viberOpenAiService = viberOpenAiService;
    }

    public async Task Handle(ViberCallbackData update)
    {
        var sender = update.Sender;
        var message = update.Message as TextMessage;
        
        _logger.LogInformation("Receive message type: {MessageType}", message.Type);
        if (message.Text is not { } messageText)
            return;

        var action = messageText.Split(' ')[0] switch
        {
            var x when x.StartsWith("--") => _keyboardService.Handle(update),
            "/help" => Usage(sender, message),
            _ => _viberOpenAiService.Handler(sender, message)
        };
        
        await action;
        // _logger.LogInformation("The message was sent with token: {SentMessageId}", sentMessage.Content.MessageToken);
    }

    private async Task SetKeyBoard(ViberUser.User sender, TextMessage message)
    {
        var newMessage = new KeyboardMessage()
        {
            //required
            Receiver = sender.Id,
            Sender = new ViberUser.User()
            {
                //required
                Name = "Our bot",
                Avatar = "https://i.imgur.com/K9SDD1X.png"
            },
            //required
            Text = "Keyboard has been set",
            Keyboard = new ViberKeyboard()
            {
                // ButtonsGroupColumns = 6,
                // ButtonsGroupRows = 1,
                DefaultHeight = false,
                BackgroundColor = "#FFFFFF",
                Buttons = new[]
                {
                    new ViberKeyboardButton()
                    {
                        Columns = 6,
                        Rows = 2,
                        BackgroundColor = "#2db9b9",
                        BackgroundMediaType = "gif",
                        BackgroundMedia = "https://confessionsofabookgeek.files.wordpress.com/2014/11/fast-reading-gif.gif",
                        BackgroundLoop = true,
                        ActionType = "reply",
                        ActionBody = "azaz",
                        // Image = "https://i.imgur.com/BIZyTI4.png",
                        Text = "Key text",
                        TextVerticalAlign = "middle",
                        TextHorizontalAlign = "center",
                        TextOpacity = 60,
                        TextSize = "regular"
                    },
                    new ViberKeyboardButton()
                    {
                        Columns = 6,
                        Rows = 2,
                        BackgroundColor = "#2db9b9",
                        BackgroundMediaType = "gif",
                        BackgroundMedia = "https://confessionsofabookgeek.files.wordpress.com/2014/11/fast-reading-gif.gif",
                        BackgroundLoop = true,
                        ActionType = "reply",
                        ActionBody = "azaz",
                        // Image = "https://i.imgur.com/BIZyTI4.png",
                        Text = "Key text",
                        TextVerticalAlign = "middle",
                        TextHorizontalAlign = "center",
                        TextOpacity = 60,
                        TextSize = "regular"
                    },
                    new ViberKeyboardButton()
                    {
                        Columns = 6,
                        Rows = 2,
                        BackgroundColor = "#2db9b9",
                        BackgroundMediaType = "gif",
                        BackgroundMedia = "https://confessionsofabookgeek.files.wordpress.com/2014/11/fast-reading-gif.gif",
                        BackgroundLoop = true,
                        ActionType = "reply",
                        ActionBody = "azaz",
                        // Image = "https://i.imgur.com/BIZyTI4.png",
                        Text = "тест",
                        TextVerticalAlign = "middle",
                        TextHorizontalAlign = "center",
                        TextOpacity = 60,
                        TextSize = "regular"
                    },
                    new ViberKeyboardButton()
                    {
                        Columns = 6,
                        Rows = 2,
                        BackgroundColor = "#2db9b9",
                        BackgroundMediaType = "gif",
                        BackgroundMedia = "https://confessionsofabookgeek.files.wordpress.com/2014/11/fast-reading-gif.gif",
                        BackgroundLoop = true,
                        ActionType = "reply",
                        ActionBody = "azaz",
                        // Image = "https://i.imgur.com/BIZyTI4.png",
                        Text = "Key text",
                        TextVerticalAlign = "middle",
                        TextHorizontalAlign = "center",
                        TextOpacity = 60,
                        TextSize = "regular"
                    }
                }
            }
        };

        // our bot returns incoming text
        await _botClient.SendMessageAsync<ViberResponse.SendMessageResponse>(newMessage);
    }


    private async Task Usage(ViberUser.User? sender, TextMessage message)
    {
        const string usage = "Write --help";

        var newMessage = new TextMessage()
        {
            //required
            Receiver = sender.Id,
            Sender = new ViberUser.User()
            {
                //required
                Name = "Our bot",
                Avatar = "https://i.imgur.com/K9SDD1X.png"
            },
            //required
            Text = usage
        };

        // our bot returns incoming text
        await _botClient.SendMessageAsync<ViberResponse.SendMessageResponse>(newMessage);
    }
}