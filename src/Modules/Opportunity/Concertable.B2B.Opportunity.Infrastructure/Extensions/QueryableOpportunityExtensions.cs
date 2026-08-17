using Concertable.B2B.Opportunity.Domain.Entities;
namespace Concertable.B2B.Opportunity.Infrastructure.Extensions;

internal static class QueryableOpportunityExtensions
{
    public static IQueryable<OpportunityEntity> WhereActive(
        this IQueryable<OpportunityEntity> query,
        DateTimeOffset now) =>
        query
            .Where(o => o.Period.Start >= now)
            .Where(o => o.State == OpportunityState.Open);

    public static IQueryable<OpportunityEntity> ActiveForVenue(
        this IQueryable<OpportunityEntity> query,
        int venueId,
        DateTimeOffset now) =>
        query
            .Where(o => o.VenueId == venueId)
            .WhereActive(now)
            .OrderBy(o => o.Period.Start);
}
