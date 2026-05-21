using Concertable.DataAccess;

namespace Concertable.Concert.Application.Workflow;

internal interface ILifecycleRepository<TEntity> : IIdRepository<TEntity>
    where TEntity : class, ILifecycleEntity;
