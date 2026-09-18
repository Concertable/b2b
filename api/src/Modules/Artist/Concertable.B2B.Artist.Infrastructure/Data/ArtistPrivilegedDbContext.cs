using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Data;

/// <summary>The unfiltered, unfenced stance for work no human is acting in. See <see cref="ArtistDbContext"/>.</summary>
internal sealed class ArtistPrivilegedDbContext(
    DbContextOptions<ArtistPrivilegedDbContext> options,
    ArtistConfigurationProvider provider)
    : PrivilegedDbContext(options, provider, Schema.Name)
{
    public DbSet<ArtistEntity> Artists => Set<ArtistEntity>();
    public DbSet<ArtistRatingProjection> ArtistRatingProjections => Set<ArtistRatingProjection>();
    public DbSet<ArtistReview> ArtistReviews => Set<ArtistReview>();
}
