using Concertable.Concert.Application.Workflow;
using Concertable.Concert.Infrastructure.Data;

namespace Concertable.Concert.Infrastructure.Repositories;

internal class LifecycleRepository<TEntity> : Repository<TEntity>, ILifecycleRepository<TEntity>
    where TEntity : class, ILifecycleEntity
{
    public LifecycleRepository(ConcertDbContext context) : base(context) { }
}
