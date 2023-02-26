using MediatR;
using Microsoft.EntityFrameworkCore.Storage;
using MyTemplate.App.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Models.Types;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Infrastructure.DataAccess;
using Telegram_AI_Bot.Infrastructure.Events.Common;

namespace Telegram_AI_Bot.Infrastructure;

internal class UnitOfWorkAdapter : IUnitOfWork
{
    private readonly AppDbContext _dbContext;
    private readonly IEventStore _eventStore;
    private readonly IMediator _mediator;

    private IUnitOfWorkTransaction? ActiveTransaction { get; set; }

    public UnitOfWorkAdapter(
        AppDbContext dbContext,
        IEventStore eventStore,
        IMediator mediator)
    {
        _dbContext = dbContext;
        _eventStore = eventStore;
        _mediator = mediator;
    }

    public async Task CommitAsync()
    {
        await FireDomainEvents();

        if (_eventStore.Any)
            await CapCommitAsync();
        else
            await NoCapCommitAsync();

        if (ActiveTransaction == null)
        {
        }
    }

    public IUnitOfWorkTransaction BeginTransaction()
    {
        var dbContextTransaction = _eventStore.BeginTransaction(_dbContext);
        return ActiveTransaction = new UnitOfWorkTransaction(dbContextTransaction, this);
    }

    private async Task CapCommitAsync()
    {
        if (ActiveTransaction != null)
        {
            await _eventStore.FlushAsync();
            await _dbContext.SaveChangesAsync();
            return;
        }

        await using var transaction = _eventStore.BeginTransaction(_dbContext);
        await _eventStore.FlushAsync();
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private async Task NoCapCommitAsync()
    {
        await _dbContext.SaveChangesAsync();
    }

    private async Task FireDomainEvents(int depth = 0)
    {
        _dbContext.ChangeTracker.DetectChanges();
        var entities = GetEntities();

        foreach (var entity in entities)
        {
            foreach (var domainEvent in entity.PopDomainEvents())
            {
                await _mediator.Publish(domainEvent);
            }
        }

        if (!HasNotFiredDomainEvents())
            return;

        if (depth > 50)
            throw new InvalidOperationException("Recursive domain events detected, maximum 50 fires exceed");

        await FireDomainEvents(depth + 1);
    }

    private IEntity[] GetEntities()
    {
        return _dbContext.ChangeTracker.Entries<IEntity>()
            .Where(x => x.Entity.HasAnyDomainEvent())
            .Select(x => x.Entity).ToArray();
    }

    private bool HasNotFiredDomainEvents()
    {
        return GetEntities().Any(x => x.HasAnyDomainEvent());
    }

    private class UnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _dbContextTransaction;
        private readonly UnitOfWorkAdapter _unitOfWork;

        public UnitOfWorkTransaction(IDbContextTransaction dbContextTransaction, UnitOfWorkAdapter unitOfWork)
        {
            _dbContextTransaction = dbContextTransaction;
            _unitOfWork = unitOfWork;
        }

        public void Dispose()
        {
            _dbContextTransaction.Dispose();
            _unitOfWork.ActiveTransaction = null;
        }

        public void Commit()
        {
            _dbContextTransaction.Commit();
            _unitOfWork.ActiveTransaction = null;
        }

        public void Rollback()
        {
            _dbContextTransaction.Rollback();
            _unitOfWork.ActiveTransaction = null;
        }
    }
}