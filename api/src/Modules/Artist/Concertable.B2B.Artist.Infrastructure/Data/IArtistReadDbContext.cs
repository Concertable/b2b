namespace Concertable.B2B.Artist.Infrastructure.Data;

internal interface IArtistReadDbContext
{
    IQueryable<ArtistEntity> Artists { get; }
    IQueryable<ArtistRatingProjection> ArtistRatingProjections { get; }
}
