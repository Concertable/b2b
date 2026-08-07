using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;

namespace Concertable.B2B.Concert.Infrastructure.Mappers;

internal static class QueryableArtistDashboardMappers
{
    public static IQueryable<ArtistDashboardCounts> ToArtistCounts(
        this IQueryable<ArtistReadModel> query,
        IQueryable<ApplicationEntity> applications,
        IQueryable<ApplicationEntity> acceptedAwaitingCheckout,
        IQueryable<ConcertEntity> upcomingConcerts)
        => query.Select(a => new ArtistDashboardCounts(
            applications.Count(),
            acceptedAwaitingCheckout.Count(),
            upcomingConcerts.Count()));
}
