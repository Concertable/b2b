using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Data;

internal sealed class VenueDbContext(
    DbContextOptions<VenueDbContext> options,
    VenueConfigurationProvider provider)
    : ReadDbContext(options, provider, Schema.Name)
{
    public DbSet<VenueEntity> Venues => Set<VenueEntity>();
    public DbSet<VenueRatingProjection> VenueRatingProjections => Set<VenueRatingProjection>();
}
