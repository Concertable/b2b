using Concertable.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Workflow;

internal interface ILifecycleRepository<TEntity> : IRepository<TEntity>
    where TEntity : class, ILifecycleEntity;
