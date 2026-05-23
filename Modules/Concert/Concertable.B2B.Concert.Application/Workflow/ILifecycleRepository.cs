using Concertable.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Workflow;

internal interface ILifecycleRepository<TEntity> : IIdRepository<TEntity>
    where TEntity : class, ILifecycleEntity;
