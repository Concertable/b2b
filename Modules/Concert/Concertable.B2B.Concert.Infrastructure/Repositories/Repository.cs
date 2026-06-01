using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.Kernel;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal abstract class BaseRepository<TEntity>(ConcertDbContext context)
    : BaseRepository<TEntity, ConcertDbContext>(context)
    where TEntity : class;

internal abstract class ReadRepository<TEntity>(ConcertDbContext context)
    : ReadRepository<TEntity, ConcertDbContext, int>(context)
    where TEntity : class, IIdEntity;

internal abstract class Repository<TEntity>(ConcertDbContext context)
    : Repository<TEntity, ConcertDbContext, int>(context)
    where TEntity : class, IIdEntity;
