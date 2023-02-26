using System.Reflection;
using DotNetCore.CAP;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Telegram_AI_Bot.Infrastructure.Services.Events.Cap.Subscriptions.Registration;

internal class SubscriptionConsumerServiceSelector : ConsumerServiceSelector
{
    private readonly CapOptions _capOptions;
    public const string PAYLOAD_PARAMETER_NAME = "payload";
    public const string CAP_HEADER_PARAMETER_NAME = "capHeader";

    public SubscriptionConsumerServiceSelector(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        _capOptions = serviceProvider.GetService<IOptions<CapOptions>>()!.Value;
    }

    private string Group => _capOptions.DefaultGroupName + "." + _capOptions.Version;

    protected override IEnumerable<ConsumerExecutorDescriptor> FindConsumersFromInterfaceTypes(
        IServiceProvider provider)
    {
        using var scoped = provider.CreateScope();
        var scopedProvider = scoped.ServiceProvider;
        var registrations = scopedProvider.GetServices<SubscriptionRegistration>();
        return registrations.Select(GetDescription).ToArray();
    }

    private ConsumerExecutorDescriptor GetDescription(SubscriptionRegistration registration)
    {
        var activatorType = ActivatorType(registration);

        return new ConsumerExecutorDescriptor
        {
            Attribute = new CapSubscribeAttribute(registration.Name)
            {
                Group = Group,
            },
            Parameters = Parameters(registration),
            MethodInfo = MethodInfo(registration),
            ImplTypeInfo = activatorType.GetTypeInfo(),
            TopicNamePrefix = _capOptions.TopicNamePrefix,
        };
    }

    private MethodInfo MethodInfo(SubscriptionRegistration registration)
    {
        var activatorType = ActivatorType(registration);
        var methodName = nameof(SubscriptionActivator<ISubscription<object>, object>.HandleAsync);
        return activatorType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
    }

    private ParameterDescriptor[] Parameters(SubscriptionRegistration registration)
    {
        var payloadParameter = new ParameterDescriptor
        {
            Name = PAYLOAD_PARAMETER_NAME,
            ParameterType = registration.PayloadType,
            IsFromCap = false,
        };

        var capHeaderParameter = new ParameterDescriptor
        {
            Name = CAP_HEADER_PARAMETER_NAME,
            ParameterType = typeof(CapHeader),
            IsFromCap = true,
        };

        return new[] { payloadParameter, capHeaderParameter };
    }

    private Type ActivatorType(SubscriptionRegistration registration)
    {
        return typeof(SubscriptionActivator<,>).MakeGenericType(registration.Subscriber, registration.PayloadType);
    }
}