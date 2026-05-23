using Concertable.Concert.Contracts;
using Concertable.Concert.Domain.Entities;
using Concertable.Concert.Domain.ReadModels;
using Concertable.Venue.Domain;

namespace Concertable.Concert.Infrastructure.Mappers;

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
