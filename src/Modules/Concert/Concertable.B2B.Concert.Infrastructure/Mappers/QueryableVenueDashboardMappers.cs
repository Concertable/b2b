using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;

namespace Concertable.B2B.Concert.Infrastructure.Mappers;

internal static class QueryableVenueDashboardMappers
{
    extension(IQueryable<VenueReadModel> query)
    {
        public IQueryable<VenueConcertDashboardCounts> ToVenueCounts(
            IQueryable<ConcertEntity> upcomingConcerts,
            IQueryable<ConcertEntity> awaitingDoorRevenue) =>
            query.Select(v => new VenueConcertDashboardCounts(
                upcomingConcerts.Count(),
                awaitingDoorRevenue.Count()));
    }
}
