using Concertable.Artist.Domain;
using Concertable.Concert.Contracts;

namespace Concertable.Concert.Infrastructure.Mappers;

internal static class QueryableArtistDashboardMappers
{
    public static IQueryable<ArtistDashboardCountsDto> ToArtistCounts(
        this IQueryable<ArtistReadModel> query,
        IQueryable<ApplicationEntity> applications,
        IQueryable<ConcertEntity> upcomingConcerts)
        => query.Select(a => new ArtistDashboardCountsDto(
            applications.Count(),
            upcomingConcerts.Count()));
}
