namespace Telegram_AI_Bot.Core.Services.Common;

public class JobRegistration
{
    public JobRegistration(string eventName, Type eventType, Type jobType)
    {
        EventName = eventName;
        EventType = eventType;
        JobType = jobType;
    }

    public string EventName { get; }
    public Type EventType { get; }
    public Type JobType { get; }
}