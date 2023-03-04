﻿using MediatR;
using MyTemplate.App.Core.Models.Types;
using OpenAI.Images;
using Telegram_AI_Bot.Core.Models.Types;
using Telegram_AI_Bot.Core.Services.OpenAi;

namespace Telegram_AI_Bot.Core.Models.Users;

public class User : IEntity, IAggregatedRoot, IHasId, IOpenAiUser
{
    protected readonly List<OpenAiMessage> _messages = new();

    protected User()
    {
    }

    public Guid Id { get; protected set; }
    public string UserId { get; protected set; }
    public Name Name { get; protected set; }
    public int Balance { get; protected set; }
    public SelectedMode SelectedMode { get; protected set; }
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
    
    public static async Task<User> NewClientAsync(
        string userId,
        Name name,
        Role role)
    {
        return await NewAsync(userId, name, role);
    }

    private static async Task<User> NewAsync(
        string userId,
        Name name,
        Role role)
    {
        Asserts.Arg(role).NotNull();
        Asserts.Arg(name).NotNull();

        var user = new User
        {
            Id = Guid.NewGuid(),
            
            UserId = userId,
            Name = name ?? throw new ArgumentNullException(nameof(name)),
            Role = role,
        };
        return user;
    }
    
    public void DeleteContext()
    {
        throw new NotImplementedException();
    }

    public void AddMessage(string text, bool isMe, DateTimeOffset time)
    {
        throw new NotImplementedException();
    }

    public void RemoveUnnecessary()
    {
        throw new NotImplementedException();
    }

    public void ReduceChatTokens(int tokens)
    {
        throw new NotImplementedException();
    }

    public void ReduceImageTokens(ImageSize imageSize, OpenAiConfiguration openAiOptions)
    {
        throw new NotImplementedException();
    }
}