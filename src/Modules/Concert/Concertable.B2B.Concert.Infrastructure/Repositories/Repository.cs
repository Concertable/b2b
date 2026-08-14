using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure;
using Concertable.Kernel;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal abstract class WriteRepository<TEntity>(TenantConcertDbContext context)
    : WriteRepository<TEntity, TenantConcertDbContext>(context)
    where TEntity : class;

internal abstract class Repository<TEntity>(TenantConcertDbContext context)
    : Repository<TEntity, TenantConcertDbContext, int>(context)
    where TEntity : class, IIdEntity;

internal abstract class TenantScopedRepository<TEntity>(TenantConcertDbContext context, ITenantContext tenant)
    : TenantScopedRepository<TEntity, TenantConcertDbContext, int>(context, tenant)
    where TEntity : class, IIdEntity, ITenantScoped;

internal abstract class VenueArtistTenantScopedRepository<TEntity>(TenantConcertDbContext context)
    : VenueArtistTenantScopedRepository<TEntity, TenantConcertDbContext, int>(context)
    where TEntity : class, IIdEntity, IVenueArtistTenantScoped;
