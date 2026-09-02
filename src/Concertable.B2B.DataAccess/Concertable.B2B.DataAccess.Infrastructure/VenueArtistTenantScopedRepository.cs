using Concertable.B2B.DataAccess.Application;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Kernel;

namespace Concertable.B2B.DataAccess.Infrastructure;

public abstract class VenueArtistTenantScopedRepository<TEntity, TKey>
    : Repository<TEntity, TKey>, IVenueArtistTenantScopedRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, IVenueArtistTenantScoped
{
    protected VenueArtistTenantScopedRepository(IDbContext context) : base(context) { }
}
