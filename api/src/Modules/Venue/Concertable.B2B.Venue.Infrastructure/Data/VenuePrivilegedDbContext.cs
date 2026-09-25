using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Venue.Infrastructure.Data;

internal sealed class VenuePrivilegedDbContext(
    DbContextOptions<VenuePrivilegedDbContext> options,
    IOptions<OutboxOptions> outboxOptions,
    VenueConfigurationProvider provider)
    : PrivilegedDbContext(options, outboxOptions, provider, Schema.Name)
{
    public DbSet<VenueEntity> Venues => Set<VenueEntity>();
    public DbSet<VenueImageEntity> VenueImages => Set<VenueImageEntity>();
    public DbSet<VenueRatingProjection> VenueRatingProjections => Set<VenueRatingProjection>();
    public DbSet<VenueReview> VenueReviews => Set<VenueReview>();
}
