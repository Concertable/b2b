using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;

namespace Concertable.B2B.Concert.Infrastructure.Mappers;

internal static class QueryableArtistDashboardMappers
{
    public static IQueryable<ArtistConcertDashboardCounts> ToArtistCounts(
        this IQueryable<ArtistReadModel> query,
        IQueryable<ConcertEntity> upcomingConcerts)
        => query.Select(a => new ArtistConcertDashboardCounts(
            upcomingConcerts.Count()));
}
