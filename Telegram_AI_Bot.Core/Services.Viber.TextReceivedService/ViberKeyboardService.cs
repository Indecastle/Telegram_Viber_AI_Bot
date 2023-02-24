using Telegram_AI_Bot.Core.Ports.DataAccess.Viber;
using Telegram_AI_Bot.Core.Viber;
using Viber.Bot.NetCore.Models;
using Viber.Bot.NetCore.RestApi;
using static Viber.Bot.NetCore.Models.ViberMessage;
using InternalViberUser = Viber.Bot.NetCore.Models.ViberUser.User;

namespace Telegram_AI_Bot.Core.Services.Viber.TextReceivedService;

public interface IViberKeyboardService
{
    Task Handle(ViberCallbackData update);
}

public class ViberKeyboardService : IViberKeyboardService
{
    private readonly IViberBotApi _botClient;
    private readonly IViberUserRepository _userRepository;

    public ViberKeyboardService(IViberBotApi botClient, IViberUserRepository userRepository)
    {
        _botClient = botClient;
        _userRepository = userRepository;
    }

    

    public async Task Handle(ViberCallbackData update)
    {
        InternalViberUser sender = update.Sender;
        TextMessage message = update.Message as TextMessage ?? throw new InvalidOperationException();

        var args = message.Text.Split(' ');
        string command = args[0];
        args = args.Skip(1).ToArray();

        // if (!text.StartsWith("--") || text.Substring("--".Length) is not { } command || !KeyboardCommands.All.Contains(command))
        if (!KeyboardCommands.All.Contains(command.ToLowerInvariant()))
            return;

        var action = command switch
        {
            KeyboardCommands.MainMenu => KeyboardMainMenu(sender, args),
            KeyboardCommands.Balance => KeyboardBalance(sender, args),
            KeyboardCommands.Settings => KeyboardSettings(sender, args),
            KeyboardCommands.Settings_SetLanguage => KeyboardSettingsLanguage(sender, args),
            KeyboardCommands.Help => KeyboardHelp(sender, args),
        };

        await action;
    }

    

    private async Task KeyboardMainMenu(InternalViberUser sender, string[] args)
    {
        var menu = ViberMessageHelper.GetKeyboardMainMenuMessage(sender);

        await _botClient.SendMessageAsync<ViberResponse.SendMessageResponse>(menu);
    }

    private async Task KeyboardBalance(InternalViberUser sender, string[] args)
    {
        var storedUser = await _userRepository.ByUserIdAsync(sender.Id);

        string text = string.Format("Баланс:\n" +
                                    "Осталось {0} запросов", storedUser.Balance);

        var newMessage = ViberMessageHelper.GetDefaultKeyboardMessage(sender, text, ViberMessageHelper.GetDefaultKeyboard(new[]
        {
            ViberMessageHelper.BackToMainMenuButton
        }));

        await _botClient.SendMessageAsync<ViberResponse.SendMessageResponse>(newMessage);
    }

    private async Task KeyboardSettings(InternalViberUser sender, string[] args)
    {
        var newMessage = ViberMessageHelper.GetDefaultKeyboardMessage(sender, "Настройки", ViberMessageHelper.GetDefaultKeyboard(new[]
        {
            new ViberKeyboardButton()
            {
                Columns = 2,
                Rows = 1,
                BackgroundColor = ViberMessageHelper.DEFAULT_BUTTON_COLOR,
                ActionType = "reply",
                ActionBody = KeyboardCommands.Settings_SetLanguage,
                Text = "Смена языка",
                TextVerticalAlign = "middle",
                TextHorizontalAlign = "center",
                TextOpacity = 60,
                TextSize = "regular"
            },
            ViberMessageHelper.BackToMainMenuButton,
        }));

        await _botClient.SendMessageAsync<ViberResponse.SendMessageResponse>(newMessage);
    }

    private async Task KeyboardSettingsLanguage(InternalViberUser sender, string[] args)
    {
        var newMessage = ViberMessageHelper.GetDefaultKeyboardMessage(sender, "Смена языка", ViberMessageHelper.GetDefaultKeyboard(new[]
        {
            new ViberKeyboardButton()
            {
                Columns = 3,
                Rows = 1,
                BackgroundColor = ViberMessageHelper.DEFAULT_BUTTON_COLOR,
                ActionType = "reply",
                ActionBody = KeyboardCommands.Settings_SetLanguage + "ru",
                Text = "Русский",
                TextVerticalAlign = "middle",
                TextHorizontalAlign = "center",
                TextOpacity = 60,
                TextSize = "regular"
            },
            new ViberKeyboardButton()
            {
                Columns = 3,
                Rows = 1,
                BackgroundColor = ViberMessageHelper.DEFAULT_BUTTON_COLOR,
                ActionType = "reply",
                ActionBody = KeyboardCommands.Settings_SetLanguage + "en",
                Text = "Английский",
                TextVerticalAlign = "middle",
                TextHorizontalAlign = "center",
                TextOpacity = 60,
                TextSize = "regular"
            },
            ViberMessageHelper.BackToMainMenuButton,
        }));

        await _botClient.SendMessageAsync<ViberResponse.SendMessageResponse>(newMessage);
    }

    private async Task KeyboardHelp(InternalViberUser sender, string[] args)
    {
        const string helpText = "Help:\n" +
                                "Чат бот проксирующий запросы в настоящий ChatGPT\n" +
                                "Этот бот пока является бета версией\n" +
                                "--mainmenu   - главное меню\n";

        var newMessage = ViberMessageHelper.GetDefaultKeyboardMessage(sender, helpText, ViberMessageHelper.GetDefaultKeyboard(new[]
        {
            ViberMessageHelper.BackToMainMenuButton,
        }));

        await _botClient.SendMessageAsync<ViberResponse.SendMessageResponse>(newMessage);
    }
}