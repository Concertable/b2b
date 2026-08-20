using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Data;

internal sealed class VenueReadDbContext(
    DbContextOptions<VenueReadDbContext> options,
    VenueConfigurationProvider provider)
    : ReadDbContext(options, provider, Schema.Name), IVenueReadDbContext
{
    IQueryable<VenueEntity> IVenueReadDbContext.Venues => Query<VenueEntity>();
    IQueryable<VenueRatingProjection> IVenueReadDbContext.VenueRatingProjections =>
        Query<VenueRatingProjection>();
}
