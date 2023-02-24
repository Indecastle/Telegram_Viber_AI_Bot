using MyTemplate.App.Core.Ports.DataAccess;

namespace Telegram_AI_Bot.Core.Ports.DataAccess;

public interface IUnitOfWork
{
    Task CommitAsync();

    IUnitOfWorkTransaction BeginTransaction();
}