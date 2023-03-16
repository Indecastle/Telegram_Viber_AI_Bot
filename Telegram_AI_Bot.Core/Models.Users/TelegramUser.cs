﻿using MediatR;
using MyTemplate.App.Core.Models.Types;
using OpenAI.Images;
using Telegram_AI_Bot.Core.Common;
using Telegram_AI_Bot.Core.Models.Types;
using Telegram_AI_Bot.Core.Services.OpenAi;

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
    public string Language { get; set; }
    public long Balance { get; set; }
    public SelectedMode SelectedMode { get; set; }
    public string? Avatar { get; protected set; }
    public Role Role { get; protected set; }
    public IReadOnlyCollection<OpenAiMessage> Messages => _messages.AsReadOnly();

    public ICollection<INotification> Events { get; } = new List<INotification>();

    public void SetName(Name name)
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

    public void SwitchMode()
    {
        SelectedMode = SelectedMode.NextMode;
    }
    
    public static async Task<TelegramUser> NewClientAsync(
        long userId,
        Name name,
        string language,
        int balance)
    {
        return await NewAsync(userId, name, language, balance,  SelectedMode.Chat, Role.CLIENT_USER);
    }

    private static async Task<TelegramUser> NewAsync(
        long userId,
        Name name,
        string language,
        int balance,
        SelectedMode selectedMode,
        Role role)
    {
        Asserts.Arg(role).NotNull();
        Asserts.Arg(name).NotNull();

        var user = new TelegramUser
        {
            Id = Guid.NewGuid(),
            
            UserId = userId,
            Name = name ?? throw new ArgumentNullException(nameof(name)),
            Language = language,
            Balance = balance,
            SelectedMode = selectedMode,
            Role = role,
        };
        return user;
    }

    public bool IsPositiveBalance()
    {
        return Balance > 0;
    }

    public bool DeleteContext()
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
        _messages.Add(new OpenAiMessage(new Guid(), text, isMe, time));
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

    public void ReduceChatTokens(int tokens)
    {
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
}