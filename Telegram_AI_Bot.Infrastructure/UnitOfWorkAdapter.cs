using Microsoft.EntityFrameworkCore.Storage;
using MyTemplate.App.Core.Ports.DataAccess;
using Telegram_AI_Bot.Core.Models.Types;
using Telegram_AI_Bot.Core.Ports.DataAccess;
using Telegram_AI_Bot.Infrastructure.DataAccess;

namespace Telegram_AI_Bot.Infrastructure;

internal class UnitOfWorkAdapter : IUnitOfWork
{
    private readonly AppDbContext _dbContext;

    private IUnitOfWorkTransaction? ActiveTransaction { get; set; }

    public UnitOfWorkAdapter(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CommitAsync()
    {
        await _dbContext.SaveChangesAsync();
    }

    public IUnitOfWorkTransaction BeginTransaction()
    {
        var dbContextTransaction = _dbContext.Database.BeginTransaction();
        return ActiveTransaction = new UnitOfWorkTransaction(dbContextTransaction, this);
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