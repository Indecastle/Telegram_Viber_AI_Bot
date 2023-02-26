namespace Telegram_AI_Bot.Core.Events;

public record ViberPostEndpointEvent(string SenderId, string SenderName, string message);