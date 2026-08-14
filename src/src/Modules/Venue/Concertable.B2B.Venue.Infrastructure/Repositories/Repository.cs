using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Kernel;
using Concertable.Kernel.Identity;
using Concertable.B2B.Venue.Infrastructure.Data;

namespace Concertable.B2B.Venue.Infrastructure.Repositories;

internal abstract class WriteRepository<TEntity>(TenantVenueDbContext context)
    : WriteRepository<TEntity, TenantVenueDbContext>(context)
    where TEntity : class;

internal abstract class Repository<TEntity>(TenantVenueDbContext context)
    : Repository<TEntity, TenantVenueDbContext, int>(context)
    where TEntity : class, IIdEntity;

internal abstract class TenantScopedRepository<TEntity>(TenantVenueDbContext context, ITenantContext tenant)
    : TenantScopedRepository<TEntity, TenantVenueDbContext, int>(context, tenant)
    where TEntity : class, IIdEntity, ITenantScoped;
