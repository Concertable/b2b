using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Data;

/// <summary>
/// The Venue module's stance for work no human is acting in: the same mapping as
/// <see cref="VenueDbContext"/>, with neither the owning-tenant filter nor the tenant write fence. Only seed
/// factories and this module's own system work may inject it.
/// </summary>
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
