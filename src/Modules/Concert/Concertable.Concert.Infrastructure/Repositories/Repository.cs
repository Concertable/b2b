using Concertable.Concert.Infrastructure.Data;

namespace Concertable.Concert.Infrastructure.Repositories;

internal abstract class BaseRepository<TEntity>(ConcertDbContext context)
    : BaseRepository<TEntity, ConcertDbContext>(context)
    where TEntity : class;

internal abstract class Repository<TEntity>(ConcertDbContext context)
    : Repository<TEntity, ConcertDbContext>(context)
    where TEntity : class, IIdEntity;

internal abstract class GuidRepository<TEntity>(ConcertDbContext context)
    : GuidRepository<TEntity, ConcertDbContext>(context)
    where TEntity : class, IGuidEntity;
