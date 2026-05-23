using Concertable.B2B.Artist.Domain;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;

namespace Concertable.B2B.Concert.Infrastructure.Mappers;

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
