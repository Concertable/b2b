using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Data;

internal sealed class ArtistReadDbContext(
    DbContextOptions<ArtistReadDbContext> options,
    ArtistConfigurationProvider provider)
    : ReadDbContext(options, provider, Schema.Name), IArtistReadDbContext
{
    IQueryable<ArtistEntity> IArtistReadDbContext.Artists => Query<ArtistEntity>();
    IQueryable<ArtistRatingProjection> IArtistReadDbContext.ArtistRatingProjections =>
        Query<ArtistRatingProjection>();
}
