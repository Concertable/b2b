using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Venue.Domain;

namespace Concertable.B2B.Concert.Infrastructure.Mappers;

internal static class QueryableVenueDashboardMappers
{
    public static IQueryable<VenueDashboardCountsDto> ToVenueCounts(
        this IQueryable<VenueReadModel> query,
        IQueryable<ApplicationEntity> applications,
        IQueryable<OpportunityEntity> openOpportunities,
        IQueryable<ConcertEntity> upcomingConcerts)
        => query.Select(v => new VenueDashboardCountsDto(
            applications.Count(),
            openOpportunities.Count(),
            upcomingConcerts.Count()));
}
