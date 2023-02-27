using MediatR;
using MyTemplate.App.Core.Models.Types;
using Telegram_AI_Bot.Core.Models.Types;

namespace Telegram_AI_Bot.Core.Models.Viber.Users;

public class ViberUser : IEntity, IAggregatedRoot, IHasId
{
    protected ViberUser()
    {
    }

    public Guid Id { get; protected set; }
    public string UserId { get; protected set; }
    public string Name { get; protected set; }
    public int Balance { get; protected set; }
    public string Language { get; protected set; }
    public SelectedMode SelectedMode { get; protected set; }
    public string MessageHistory { get; protected set; }
    public string? Avatar { get; protected set; }
    public Role Role { get; protected set; }
    
    public ICollection<INotification> Events { get; } = new List<INotification>();

    public void SetName(string name)
    {
        Name = name;
    }

    public void ChangeAvatar(string avatarReference)
    {
        Avatar = avatarReference ?? throw new ArgumentNullException(nameof(avatarReference));
    }

    public void SetRole(Role role)
    {
        var previousRole = Role;

        if (Role == role)
            return;

        Role = role;
    }
    
    public void SetLanguage(string lang)
    {
        Language = lang;
    }
    
    public void SetSelectedMode(SelectedMode selectedMode)
    {
        SelectedMode = selectedMode;
    }
    
    public void SwitchMode()
    {
        SelectedMode = SelectedMode.NextMode;
    }
    
    public static async Task<ViberUser> NewClientAsync(
        string userId,
        string name,
        string language,
        int balance)
    {
        return await NewAsync(userId, name, language, balance,  SelectedMode.Chat, "", Role.CLIENT_USER);
    }

    private static async Task<ViberUser> NewAsync(
        string userId,
        string name,
        string language,
        int balance,
        SelectedMode selectedMode,
        string messageHistory,
        Role role)
    {
        Asserts.Arg(role).NotNull();
        Asserts.Arg(name).NotNull();

        var user = new ViberUser
        {
            Id = Guid.NewGuid(),
            
            UserId = userId,
            Name = name ?? throw new ArgumentNullException(nameof(name)),
            Language = language,
            Balance = balance,
            SelectedMode = selectedMode,
            MessageHistory = messageHistory,
            Role = role,
        };
        return user;
    }

    public bool TryDecrementBalance(int amount = 1)
    {
        if (Balance == 0 || Balance < amount)
            return false;
        
        Balance -= amount;
        return true;
    }

    public void DeleteContext()
    {
        MessageHistory = string.Empty;
    }
}