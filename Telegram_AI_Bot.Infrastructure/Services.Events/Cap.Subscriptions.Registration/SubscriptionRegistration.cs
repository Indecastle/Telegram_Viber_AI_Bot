namespace Telegram_AI_Bot.Infrastructure.Services.Events.Cap.Subscriptions.Registration;

public class SubscriptionRegistration
{
    public SubscriptionRegistration(Type subscriber, string name)
    {
        Subscriber = subscriber;
        Name = name;
    }

    public Type Subscriber { get; }

    public string Name { get; }

    private Type ClosedInterface => Subscriber
        .GetInterfaces()
        .SingleOrDefault(x =>
            x.IsInterface && x.IsConstructedGenericType && x.GetGenericTypeDefinition() == typeof(ISubscription<>));

    public Type PayloadType => ClosedInterface.GetGenericArguments()[0];
}