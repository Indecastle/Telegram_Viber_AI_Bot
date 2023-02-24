using System;

namespace MyTemplate.App.Core.Ports.DataAccess;

public interface IUnitOfWorkTransaction : IDisposable
{
    public void Commit();
    public void Rollback();
}