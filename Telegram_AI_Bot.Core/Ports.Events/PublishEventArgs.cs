namespace Telegram_AI_Bot.Core.Ports.Events;

public class PublishEventArgs
{
    public PublishEventArgs(PublishEventArgs eventArgs)
        : this(
            eventArgs == null
                ? throw new ArgumentNullException(nameof(eventArgs))
                : eventArgs.Topic,
            eventArgs.Payload,
            eventArgs.MessageId)
    {
    }

    public PublishEventArgs(string topic, object payload = null, string messageId = null)
    {
        Topic = topic ?? throw new ArgumentNullException(nameof(topic));
        Payload = payload;
        MessageId = messageId;
    }

    public string Topic { get; }

    public object Payload { get; }

    public string MessageId { get; private set; }

    public PublishEventArgs WithMessageId(string messageId)
    {
        return new PublishEventArgs(this)
        {
            MessageId = messageId ?? throw new ArgumentNullException(nameof(messageId)),
        };
    }
}