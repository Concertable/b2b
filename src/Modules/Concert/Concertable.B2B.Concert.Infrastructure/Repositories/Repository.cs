using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure;
using Concertable.Kernel;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal abstract class WriteRepository<TEntity>(ConcertTenantDbContext context)
    : WriteRepository<TEntity, ConcertTenantDbContext>(context)
    where TEntity : class;

internal abstract class Repository<TEntity>(ConcertTenantDbContext context)
    : Repository<TEntity, ConcertTenantDbContext, int>(context)
    where TEntity : class, IIdEntity;

internal abstract class TenantScopedRepository<TEntity>(ConcertTenantDbContext context, ITenantContext tenant)
    : TenantScopedRepository<TEntity, ConcertTenantDbContext, int>(context, tenant)
    where TEntity : class, IIdEntity, ITenantScoped;

internal abstract class VenueArtistTenantScopedRepository<TEntity>(ConcertTenantDbContext context)
    : VenueArtistTenantScopedRepository<TEntity, ConcertTenantDbContext, int>(context)
    where TEntity : class, IIdEntity, IVenueArtistTenantScoped;
