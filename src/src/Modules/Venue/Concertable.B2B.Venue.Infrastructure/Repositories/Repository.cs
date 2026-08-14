using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Kernel;
using Concertable.Kernel.Identity;
using Concertable.B2B.Venue.Infrastructure.Data;

namespace Concertable.B2B.Venue.Infrastructure.Repositories;

internal abstract class WriteRepository<TEntity>(VenueTenantDbContext context)
    : WriteRepository<TEntity, VenueTenantDbContext>(context)
    where TEntity : class;

internal abstract class Repository<TEntity>(VenueTenantDbContext context)
    : Repository<TEntity, VenueTenantDbContext, int>(context)
    where TEntity : class, IIdEntity;

internal abstract class TenantScopedRepository<TEntity>(VenueTenantDbContext context, ITenantContext tenant)
    : TenantScopedRepository<TEntity, VenueTenantDbContext, int>(context, tenant)
    where TEntity : class, IIdEntity, ITenantScoped;
