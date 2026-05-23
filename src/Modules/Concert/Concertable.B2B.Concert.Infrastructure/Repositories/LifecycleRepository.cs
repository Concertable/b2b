using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Infrastructure.Data;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal class LifecycleRepository<TEntity> : Repository<TEntity>, ILifecycleRepository<TEntity>
    where TEntity : class, ILifecycleEntity
{
    public LifecycleRepository(ConcertDbContext context) : base(context) { }
}
