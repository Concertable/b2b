using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Data;

/// <summary>
/// The Artist module's public stance: the same anemic configuration as <see cref="ArtistDbContext"/>
/// with no tenant filter composed on top. Injected by <c>PublicArtistRepository</c> for marketplace
/// browse (any artist's summary/details/genres) — read-only by construction. The tenant-filtered
/// counterpart is <see cref="ArtistDbContext"/>.
/// </summary>
internal sealed class PublicArtistDbContext(
    DbContextOptions<PublicArtistDbContext> options,
    ArtistConfigurationProvider provider)
    : PublicDbContext(options, provider, Schema.Name)
{
    public DbSet<ArtistEntity> Artists => Set<ArtistEntity>();
    public DbSet<ArtistRatingProjection> ArtistRatingProjections => Set<ArtistRatingProjection>();
}
