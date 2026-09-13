using Concertable.B2B.Application.Infrastructure.Data;

namespace Concertable.B2B.Application.Infrastructure.Repositories;

internal abstract class VenueArtistTenantScopedRepository<TEntity>(ApplicationDbContext context)
    : VenueArtistTenantScopedRepository<TEntity, int>(context)
    where TEntity : class, IIdEntity, IVenueArtistTenantScoped;
