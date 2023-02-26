using CorrelationId.Abstractions;
using DotNetCore.CAP;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Telegram_AI_Bot.Core.Ports.Events;

namespace Telegram_AI_Bot.Infrastructure.Events.Common;

internal class CapEventStore : IEventStore, IEventPublisherPort
{
    private readonly ICapPublisher _capPublisher;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;
    private readonly LinkedList<PublishEventArgs> _events = new();

    public CapEventStore(
        ICapPublisher capPublisher,
        ICorrelationContextAccessor correlationContextAccessor)
    {
        _capPublisher = capPublisher;
        _correlationContextAccessor = correlationContextAccessor;
    }

    public Task<string> EnqueueAsync(PublishEventArgs args)
    {
        args = args ?? throw new ArgumentNullException(nameof(args));
        if (string.IsNullOrWhiteSpace(args.MessageId))
            args = args.WithMessageId(Guid.NewGuid().ToString());

        _events.AddLast(args);
        return Task.FromResult(args.MessageId);
    }

    public bool Any => _events.Any();

    public IDbContextTransaction BeginTransaction<TDbContext>(TDbContext dbContext) where TDbContext : DbContext
    {
        return dbContext.Database.BeginTransaction(_capPublisher);
    }

    public async Task FlushAsync()
    {
        foreach (var @event in _events.ToArray())
        {
            var headers = new Dictionary<string, string>
            {
                ["MessageId"] = @event.MessageId,
            };
            
            if (_correlationContextAccessor.CorrelationContext != null)
                headers.Add("CorrelationId", _correlationContextAccessor.CorrelationContext.CorrelationId);

            await _capPublisher.PublishAsync(@event.Topic, @event.Payload, headers);
        }
    }
}