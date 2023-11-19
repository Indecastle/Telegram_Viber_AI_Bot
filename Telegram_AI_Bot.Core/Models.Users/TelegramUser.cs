using MyTemplate.App.Core.Models.Types;
using OpenAI.Images;
using Telegram.Bot.Types;
using Telegram_AI_Bot.Core.Common;
using Telegram_AI_Bot.Core.Models.Types;
using Telegram_AI_Bot.Core.Services.OpenAi;
using Telegram_AI_Bot.Core.Telegram;
using Telegram.Bot.Types.Enums;

namespace Telegram_AI_Bot.Core.Models.Users;

public class TelegramUser : IEntity, IAggregatedRoot, IHasId, IOpenAiUser
{
    protected readonly List<OpenAiMessage> _messages = new();

    protected TelegramUser()
    {
    }

    public Guid Id { get; protected set; }
    public long UserId { get; protected set; }
    public Name Name { get; protected set; }
    public string? Username { get; protected set; }
    public string Language { get; protected set; }
    public long Balance { get; protected set; }
    public string? SystemMessage { get; protected set; }
    public ChatModel? ChatModel { get; protected set; }
    public bool EnabledContext { get; protected set; }
    public bool EnabledStreamingChat { get; protected set; }
    public SelectedMode SelectedMode { get; set; }
    public WaitState? WaitState { get; set; }
    public string? Avatar { get; protected set; }
    public Role Role { get; protected set; }
    public DateTimeOffset StartAt { get; protected set; }
    public bool IsTyping { get; protected set; }
    public DateTimeOffset? LastTypingAt { get; protected set; }
    public IReadOnlyCollection<OpenAiMessage> Messages => _messages.AsReadOnly();

    public void SetName(Name name)
    {
        Name = name;
    }
    
    public void SetUserName(string userName)
    {
        Username = userName;
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

    public void SetSystemMessage(string? message)
    {
        Asserts.Arg(message)!.NotNullOrWhiteSpace();
        SystemMessage = message;
    }
    
    public void ResetWaitState()
    {
        WaitState = null;
    }
    
    public void SetWaitState(WaitState state)
    {
        if (state != WaitState)
            WaitState = state;
    }
    
    public void ResetSystemMessage()
    {
        SystemMessage = null;
    }
    
    public void SetChatModel(ChatModel model)
    {
        ChatModel = model;
        if (model == ChatModel.Gpt4)
        {
            EnabledStreamingChat = true;
        }
    }
    
    public void SetLanguage(string lang)
    {
        Language = lang;
    }

    public void SwitchMode()
    {
        SelectedMode = SelectedMode.NextMode;
    }
    
    public void SwitchEnablingContext(bool? value = null)
    {
        EnabledContext = value ?? !EnabledContext;
    }
    
    public void SwitchEnabledStreamingChat()
    {
        EnabledStreamingChat = ChatModel == ChatModel.Gpt4 || !EnabledStreamingChat;
    }
    
    public static async Task<TelegramUser> NewClientAsync(
        long userId,
        string? userName,
        Name name,
        string language,
        int balance,
        bool enabledContext)
    {
        return await NewAsync(userId, userName, name, language, balance,  SelectedMode.Chat, Role.CLIENT_USER, enabledContext, true, DateTimeOffset.UtcNow);
    }

    private static async Task<TelegramUser> NewAsync(
        long userId,
        string? userName,
        Name name,
        string language,
        int balance,
        SelectedMode selectedMode,
        Role role,
        bool enabledContext,
        bool enabledStreamingChat,
        DateTimeOffset startAt)
    {
        Asserts.Arg(role).NotNull();
        Asserts.Arg(name).NotNull();

        var user = new TelegramUser
        {
            Id = Guid.NewGuid(),
            
            UserId = userId,
            Name = name ?? throw new ArgumentNullException(nameof(name)),
            Username = userName,
            Language = language,
            Balance = balance,
            SelectedMode = selectedMode,
            Role = role,
            EnabledContext = enabledContext,
            EnabledStreamingChat = enabledStreamingChat,
            StartAt = startAt,
            ChatModel = null,
        };
        return user;
    }

    public bool IsPositiveBalance()
    {
        return Balance > 0;
    }

    public bool ClearContext()
    {
        if (_messages.Count > 0)
        {
            _messages.Clear();
            return true;
        }

        return false;
    }

    public void AddMessage(string text, bool isMe, DateTimeOffset time)
    {
        _messages.Add(new OpenAiMessage(new Guid(), text, isMe, MessageType.Text, time));
    }
    
    public void AddPhoto(string text, DateTimeOffset time)
    {
        _messages.Add(new OpenAiMessage(new Guid(), text, true, MessageType.Photo, time));
    }

    public void RemoveUnnecessary()
    {
        if (_messages.Count <= Constants.MAX_STORED_MESSAGES)
            return;
        
        foreach (var message in _messages.OrderBy(x => x.CreatedAt).Take(_messages.Count - Constants.MAX_STORED_MESSAGES))
        {
            _messages.Remove(message);
        }
    }

    public void ReduceChatTokens(int tokens, OpenAiConfiguration openAiOptions)
    {
        // int factor = ChatModel switch
        // {
        //     var x when x == ChatModel.Gpt4 => openAiOptions.FactorTextGpt4!.Value,
        //     _ => 1
        // };
        
        Balance -= tokens;
        Balance = Balance < 0 ? 0 : Balance;
    }

    public void ReduceImageTokens(ImageSize imageSize, OpenAiConfiguration openAiOptions)
    {
        Balance -= openAiOptions.FactorImage!.Value * imageSize switch
        {
            ImageSize.Small => openAiOptions.ImageSmallTokens!.Value,
            ImageSize.Medium => openAiOptions.ImageMediumTokens!.Value,
            ImageSize.Large => openAiOptions.ImageLargeTokens!.Value,
        };
        Balance = Balance < 0 ? 0 : Balance;
    }

    public void SetBalance(int amount)
    {
        Balance = amount;
    }

    public bool IsEnabledContext() => EnabledContext;

    public bool IsEnabledStreamingChat() => EnabledStreamingChat;

    public bool IsNeedUpdateBaseInfo(User internalUser) =>
        internalUser.FirstName != Name.FirstName
        || internalUser.LastName != Name.LastName
        || internalUser.Username != Username
        || IsTypingNeedToReset();

    public bool IsTypingNeedToReset() =>
        IsTyping && LastTypingAt < DateTimeOffset.UtcNow - TimeSpan.FromSeconds(Constants.LIFETIME_OF_TYPING);

    public void UpdateBaseInfo(User internalUser)
    {
        Name = new Name(internalUser.FirstName, internalUser.LastName);
        Username = internalUser.Username;
        if (IsTypingNeedToReset())
            SetTyping(false);
    }

    public void IncreaseBalance(long amount)
    {
        Balance += amount;
    }

    public bool ReduceContextIfNeed(string messageText, IOpenAiService openAiService)
    {
        if (ChatModel != ChatModel.Gpt35)
            return false;
        
        var reduced = false;
        while (TelegramMessageHelper.GetNumTokensFromMessages(ChatModel,
                   openAiService.GetChatRequest(messageText, this).Messages) > 15000)
        {
            _messages.RemoveAt(0);
            _messages.RemoveAt(0);
            reduced = true;
        }

        return reduced;
    }

    public void SetTyping(bool value)
    {
        IsTyping = value;
        LastTypingAt = DateTimeOffset.UtcNow;
    }
}