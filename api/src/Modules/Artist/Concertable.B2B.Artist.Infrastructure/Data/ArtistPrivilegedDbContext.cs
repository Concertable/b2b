using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Data;

/// <summary>
/// The Artist module's stance for work no human is acting in: the same mapping as
/// <see cref="ArtistDbContext"/>, with neither the owning-tenant filter nor the tenant write fence, because
/// this stance exists precisely to read and write rows belonging to many tenants. Only seed factories and
/// this module's own system work may inject it.
/// </summary>
internal sealed class ArtistPrivilegedDbContext(
    DbContextOptions<ArtistPrivilegedDbContext> options,
    ArtistConfigurationProvider provider)
    : PrivilegedDbContext(options, provider, Schema.Name)
{
    public DbSet<ArtistEntity> Artists => Set<ArtistEntity>();
    public DbSet<ArtistRatingProjection> ArtistRatingProjections => Set<ArtistRatingProjection>();
    public DbSet<ArtistReview> ArtistReviews => Set<ArtistReview>();
}
