using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure;
using Concertable.Kernel;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal abstract class Repository<TEntity>(ConcertDbContext context)
    : Repository<TEntity, int>(context)
    where TEntity : class, IIdEntity;

internal abstract class TenantScopedRepository<TEntity>(ConcertDbContext context, ITenantContext tenant)
    : TenantScopedRepository<TEntity, int>(context, tenant)
    where TEntity : class, IIdEntity, ITenantScoped;

internal abstract class VenueArtistTenantScopedRepository<TEntity>(ConcertDbContext context)
    : VenueArtistTenantScopedRepository<TEntity, int>(context)
    where TEntity : class, IIdEntity, IVenueArtistTenantScoped;
