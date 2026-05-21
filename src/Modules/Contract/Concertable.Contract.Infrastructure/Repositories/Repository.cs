using Concertable.Contract.Infrastructure.Data;

namespace Concertable.Contract.Infrastructure.Repositories;

internal abstract class BaseRepository<TEntity>(ContractDbContext context)
    : BaseRepository<TEntity, ContractDbContext>(context)
    where TEntity : class;

internal abstract class Repository<TEntity>(ContractDbContext context)
    : Repository<TEntity, ContractDbContext>(context)
    where TEntity : class, IIdEntity;

internal abstract class GuidRepository<TEntity>(ContractDbContext context)
    : GuidRepository<TEntity, ContractDbContext>(context)
    where TEntity : class, IGuidEntity;
