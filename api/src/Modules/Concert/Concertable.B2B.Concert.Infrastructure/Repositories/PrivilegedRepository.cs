using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure;
using Concertable.Kernel;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal abstract class PrivilegedRepository<TEntity>(ConcertPrivilegedDbContext context)
    : Repository<TEntity, int>(context)
    where TEntity : class, IIdEntity;
