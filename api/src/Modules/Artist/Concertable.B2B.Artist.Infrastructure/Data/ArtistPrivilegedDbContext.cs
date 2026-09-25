using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Artist.Infrastructure.Data;

internal sealed class ArtistPrivilegedDbContext(
    DbContextOptions<ArtistPrivilegedDbContext> options,
    IOptions<OutboxOptions> outboxOptions,
    ArtistConfigurationProvider provider)
    : PrivilegedDbContext(options, outboxOptions, provider, Schema.Name)
{
    public DbSet<ArtistEntity> Artists => Set<ArtistEntity>();
    public DbSet<ArtistRatingProjection> ArtistRatingProjections => Set<ArtistRatingProjection>();
    public DbSet<ArtistReview> ArtistReviews => Set<ArtistReview>();
}
