using Askmethat.Aspnet.JsonLocalizer.Localizer;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Telegram_AI_Bot.Core.Models;
using Telegram_AI_Bot.Core.Ports.DataAccess.Viber;
using Telegram_AI_Bot.Core.Viber;
using Telegram_AI_Bot.Core.Viber.Options;
using Viber.Bot.NetCore.Models;
using Viber.Bot.NetCore.RestApi;
using static Viber.Bot.NetCore.Models.ViberMessage;
using InternalViberUser = Viber.Bot.NetCore.Models.ViberUser.User;
using ViberUser = Telegram_AI_Bot.Core.Models.Viber.Users.ViberUser;

namespace Telegram_AI_Bot.Core.Services.Viber.TextReceivedService;

public interface IViberKeyboardService
{
    Task Handle(ViberCallbackData update);
}

public class ViberKeyboardService : IViberKeyboardService
{
    private readonly IViberBotApi _botClient;
    private readonly IViberUserRepository _userRepository;
    private readonly IJsonStringLocalizer _localizer;
    private readonly ViberBotConfiguration _viberOptions;

    public ViberKeyboardService(
        IViberBotApi botClient,
        IViberUserRepository userRepository,
        IJsonStringLocalizer localizer,
        IOptions<ViberBotConfiguration> viberOptions)
    {
        _botClient = botClient;
        _userRepository = userRepository;
        _localizer = localizer;
        _viberOptions = viberOptions.Value;
    }

    

    public async Task Handle(ViberCallbackData update)
    {
        InternalViberUser sender = update.Sender;
        TextMessage message = update.Message as TextMessage ?? throw new InvalidOperationException();

        var args = message.Text.Split(' ');
        string command = args[0];
        args = args.Skip(1).ToArray();

        if (!ViberKeyboardCommands.All.Contains(command.ToLowerInvariant()))
            return;

        var user = await _userRepository.ByUserIdAsync(sender.Id);
        
        var action = command switch
        {
            ViberKeyboardCommands.MainMenu => KeyboardMainMenu(sender, args),
            ViberKeyboardCommands.Balance => KeyboardBalance(sender, args),
            ViberKeyboardCommands.Settings => KeyboardSettings(sender, user, args),
            ViberKeyboardCommands.Settings_SetLanguage => KeyboardSettingsLanguage(sender, user, args),
            ViberKeyboardCommands.Help => KeyboardHelp(sender, user, args),
        };

        await action;
    }

    private async Task KeyboardMainMenu(InternalViberUser sender, string[] args)
    {
        var menu = ViberMessageHelper.GetKeyboardMainMenuMessage(_localizer, sender, _localizer.GetString("MainMenu"));
        // menu.MinApiVersion = 6;
        await _botClient.SendMessageV6Async(menu);
    }

    private async Task KeyboardBalance(InternalViberUser sender, string[] args)
    {
        var storedUser = await _userRepository.ByUserIdAsync(sender.Id);

        string text = _localizer.GetString("Balance", storedUser.Balance);

        var newMessage = ViberMessageHelper.GetDefaultKeyboardMessage(sender, text, ViberMessageHelper.GetDefaultKeyboard(new[]
        {
            ViberMessageHelper.BackToMainMenuButton(_localizer)
        }));

        await _botClient.SendMessageV6Async(newMessage);
    }

    private async Task KeyboardSettings(InternalViberUser sender, ViberUser user, string[] args)
    {
        var text = _localizer.GetString("Settings");
        switch (args.FirstOrDefault())
        {
            case "SwitchMode": user.SwitchMode();
                break;
            case "ClearContext": 
                user.ClearContext();
                text = _localizer.GetString("DeletedContext");
                break;
        }

        string? gradientColor = user.SelectedMode == SelectedMode.Chat ? "#003cb3" : "#e63900";
        
        var newMessage = ViberMessageHelper.GetDefaultKeyboardMessage(sender, text,
            ViberMessageHelper.GetDefaultKeyboard(new[]
            {
                ViberMessageHelper.GetDefaultKeyboardButton(2, 3, _localizer.GetString("ChangeLanguage"),
                    ViberKeyboardCommands.Settings_SetLanguage,
                    textVerticalAlign: "bottom",
                    textBackgroundGradientColor: "#003cb3",
                    image: "https://i.imgur.com/ATcqFri.png"),

                ViberMessageHelper.GetDefaultKeyboardButton(2, 3, 
                    _localizer.GetString("SelectedMode_" + user.SelectedMode.Value),
                    ViberKeyboardCommands.WithArgs(ViberKeyboardCommands.Settings, "SwitchMode"),
                    textVerticalAlign: "bottom",
                    textSize: "regular",
                    textBackgroundGradientColor: gradientColor,
                    image: "https://i.imgur.com/RFxB3Wa.png"),
                
                ViberMessageHelper.GetDefaultKeyboardButton(2, 3, _localizer.GetString("ClearContext"),
                    ViberKeyboardCommands.WithArgs(ViberKeyboardCommands.Settings, "ClearContext"),
                    textVerticalAlign: "bottom",
                    textBackgroundGradientColor: "#003cb3",
                    image: "https://i.imgur.com/4Qe65rF.png"),
                
                ViberMessageHelper.BackToMainMenuButton(_localizer),
            }));

        await _botClient.SendMessageV6Async(newMessage);
    }

    private async Task KeyboardSettingsLanguage(InternalViberUser sender, ViberUser user, string[] args)
    {
        if (args.Any())
        {
            user.SetLanguage(args.First());
            ViberMessageHelper.SetCulture(user.Language);
        }

        var newMessage = ViberMessageHelper.GetDefaultKeyboardMessage(sender, _localizer.GetString("ChangeLanguage"),
            ViberMessageHelper.GetDefaultKeyboard(new[]
            {
                ViberMessageHelper.GetDefaultKeyboardButton(3, 1, _localizer.GetString("Russian"),
                    ViberKeyboardCommands.WithArgs(ViberKeyboardCommands.Settings_SetLanguage, "ru-RU")),
                ViberMessageHelper.GetDefaultKeyboardButton(3, 1, _localizer.GetString("English"),
                    ViberKeyboardCommands.WithArgs(ViberKeyboardCommands.Settings_SetLanguage, "en-US")),
                ViberMessageHelper.BackToPrevMenuButton(_localizer, ViberKeyboardCommands.Settings),
            }));

        await _botClient.SendMessageV6Async(newMessage);
    }

    private async Task KeyboardHelp(InternalViberUser sender, ViberUser user, string[] args)
    {
        var newMessage = ViberMessageHelper.GetDefaultKeyboardMessage(sender, _localizer.GetString("HelpText"), ViberMessageHelper.GetDefaultKeyboard(new[]
        {
            ViberMessageHelper.GetDefaultKeyboardButton(3, 1, _localizer.GetString("GetContact"),
                ViberKeyboardCommands.WithArgs(ViberKeyboardCommands.Help, "GetAdminContact")),
            ViberMessageHelper.GetDefaultKeyboardButton(3, 1, _localizer.GetString("MyId"),
                ViberKeyboardCommands.WithArgs(ViberKeyboardCommands.Help, "MyId")),
            ViberMessageHelper.BackToMainMenuButton(_localizer),
        }));
        
        switch (args.FirstOrDefault())
        {
            case "GetAdminContact":
                var contactMessage = new ViberContactMessageV6(newMessage);
                contactMessage.Contact = new()
                {
                    Name = "Owner",
                    PhoneNumber = _viberOptions.AdminPhoneNumber
                };
                await _botClient.SendMessageV6Async(contactMessage);
                return;
            case "MyId": 
                newMessage.Text = user.UserId;
                break;
        }

        await _botClient.SendMessageV6Async(newMessage);
    }
}