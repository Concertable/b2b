using Concertable.Customer.Infrastructure.Data;

namespace Concertable.Customer.Infrastructure.Repositories;

internal abstract class BaseRepository<TEntity>(CustomerDbContext context)
    : BaseRepository<TEntity, CustomerDbContext>(context)
    where TEntity : class;

internal abstract class Repository<TEntity>(CustomerDbContext context)
    : Repository<TEntity, CustomerDbContext>(context)
    where TEntity : class, IIdEntity;

internal abstract class GuidRepository<TEntity>(CustomerDbContext context)
    : GuidRepository<TEntity, CustomerDbContext>(context)
    where TEntity : class, IGuidEntity;
