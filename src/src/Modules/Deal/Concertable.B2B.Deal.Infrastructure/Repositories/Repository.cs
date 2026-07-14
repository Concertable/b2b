using Concertable.B2B.Deal.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Kernel;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Deal.Infrastructure.Repositories;

internal abstract class BaseRepository<TEntity>(DealDbContext context)
    : BaseRepository<TEntity, DealDbContext>(context)
    where TEntity : class;

internal abstract class ReadRepository<TEntity>(DealDbContext context)
    : ReadRepository<TEntity, DealDbContext, int>(context)
    where TEntity : class, IIdEntity;

internal abstract class Repository<TEntity>(DealDbContext context)
    : Repository<TEntity, DealDbContext, int>(context)
    where TEntity : class, IIdEntity;

internal abstract class TenantScopedRepository<TEntity>(DealDbContext context, ITenantContext tenant)
    : TenantScopedRepository<TEntity, DealDbContext, int>(context, tenant)
    where TEntity : class, IIdEntity, ITenantScoped;
