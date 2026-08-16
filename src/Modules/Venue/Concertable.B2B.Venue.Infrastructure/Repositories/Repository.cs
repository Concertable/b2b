using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Kernel;
using Concertable.Kernel.Identity;
using Concertable.B2B.Venue.Infrastructure.Data;

namespace Concertable.B2B.Venue.Infrastructure.Repositories;

internal abstract class WriteRepository<TEntity>(VenueDbContext context)
    : WriteRepository<TEntity, VenueDbContext>(context)
    where TEntity : class;

internal abstract class ReadRepository<TEntity>(VenueDbContext context)
    : ReadRepository<TEntity, int>(context)
    where TEntity : class, IIdEntity;

internal abstract class Repository<TEntity>(VenueDbContext context)
    : Repository<TEntity, VenueDbContext, int>(context)
    where TEntity : class, IIdEntity;

internal abstract class TenantScopedRepository<TEntity>(VenueDbContext context, ITenantContext tenant)
    : TenantScopedRepository<TEntity, VenueDbContext, int>(context, tenant)
    where TEntity : class, IIdEntity, ITenantScoped;
