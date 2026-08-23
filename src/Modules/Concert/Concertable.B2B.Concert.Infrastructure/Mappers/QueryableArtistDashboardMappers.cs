using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;

namespace Concertable.B2B.Concert.Infrastructure.Mappers;

internal static class QueryableArtistDashboardMappers
{
    extension(IQueryable<ArtistReadModel> query)
    {
        public IQueryable<ArtistConcertDashboardCounts> ToArtistCounts(
            IQueryable<ConcertEntity> upcomingConcerts) =>
            query.Select(a => new ArtistConcertDashboardCounts(
                upcomingConcerts.Count()));
    }
}
