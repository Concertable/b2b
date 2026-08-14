using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Data;

internal sealed class ArtistDbContext(
    DbContextOptions<ArtistDbContext> options,
    ArtistConfigurationProvider provider)
    : ReadDbContext(options, provider, Schema.Name)
{
    public DbSet<ArtistEntity> Artists => Set<ArtistEntity>();
    public DbSet<ArtistRatingProjection> ArtistRatingProjections => Set<ArtistRatingProjection>();
}
