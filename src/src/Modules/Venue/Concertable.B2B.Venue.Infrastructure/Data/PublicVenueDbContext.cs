using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Data;

/// <summary>
/// The Venue module's public stance: the same anemic configuration as <see cref="VenueDbContext"/>
/// with no tenant filter composed on top. Injected by <c>PublicVenueRepository</c> for marketplace
/// browse (any venue's summary/details page) — read-only by construction. The tenant-filtered
/// counterpart is <see cref="VenueDbContext"/>.
/// </summary>
internal sealed class PublicVenueDbContext(
    DbContextOptions<PublicVenueDbContext> options,
    VenueConfigurationProvider provider)
    : PublicDbContext(options, provider, Schema.Name)
{
    public DbSet<VenueEntity> Venues => Set<VenueEntity>();
    public DbSet<VenueRatingProjection> VenueRatingProjections => Set<VenueRatingProjection>();
}
