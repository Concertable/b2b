using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Kernel;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Artist.Infrastructure.Repositories;

internal abstract class WriteRepository<TEntity>(TenantArtistDbContext context)
    : WriteRepository<TEntity, TenantArtistDbContext>(context)
    where TEntity : class;

internal abstract class Repository<TEntity>(TenantArtistDbContext context)
    : Repository<TEntity, TenantArtistDbContext, int>(context)
    where TEntity : class, IIdEntity;

internal abstract class TenantScopedRepository<TEntity>(TenantArtistDbContext context, ITenantContext tenant)
    : TenantScopedRepository<TEntity, TenantArtistDbContext, int>(context, tenant)
    where TEntity : class, IIdEntity, ITenantScoped;
