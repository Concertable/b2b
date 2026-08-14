using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Kernel;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Artist.Infrastructure.Repositories;

internal abstract class WriteRepository<TEntity>(ArtistTenantDbContext context)
    : WriteRepository<TEntity, ArtistTenantDbContext>(context)
    where TEntity : class;

internal abstract class Repository<TEntity>(ArtistTenantDbContext context)
    : Repository<TEntity, ArtistTenantDbContext, int>(context)
    where TEntity : class, IIdEntity;

internal abstract class TenantScopedRepository<TEntity>(ArtistTenantDbContext context, ITenantContext tenant)
    : TenantScopedRepository<TEntity, ArtistTenantDbContext, int>(context, tenant)
    where TEntity : class, IIdEntity, ITenantScoped;
