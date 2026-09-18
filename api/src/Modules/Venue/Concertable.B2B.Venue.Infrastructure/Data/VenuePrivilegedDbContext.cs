using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Data;

/// <summary>The unfiltered, unfenced stance for work no human is acting in. See <see cref="VenueDbContext"/>.</summary>
internal sealed class VenuePrivilegedDbContext(
    DbContextOptions<VenuePrivilegedDbContext> options,
    VenueConfigurationProvider provider)
    : PrivilegedDbContext(options, provider, Schema.Name)
{
    public DbSet<VenueEntity> Venues => Set<VenueEntity>();
    public DbSet<VenueImageEntity> VenueImages => Set<VenueImageEntity>();
    public DbSet<VenueRatingProjection> VenueRatingProjections => Set<VenueRatingProjection>();
    public DbSet<VenueReview> VenueReviews => Set<VenueReview>();
}
