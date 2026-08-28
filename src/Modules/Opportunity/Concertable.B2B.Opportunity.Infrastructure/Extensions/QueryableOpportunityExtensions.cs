using Concertable.B2B.Opportunity.Domain.Entities;

namespace Concertable.B2B.Opportunity.Infrastructure.Extensions;

internal static class QueryableOpportunityExtensions
{
    extension(IQueryable<OpportunityEntity> query)
    {
        public IQueryable<OpportunityEntity> WhereActive(DateTime now) =>
            query
                .Where(o => o.Period.Start >= now)
                .Where(o => o.State == OpportunityState.Open);

        public IQueryable<OpportunityEntity> ActiveForVenue(
            int venueId,
            DateTime now) =>
            query
                .Where(o => o.VenueId == venueId)
                .WhereActive(now)
                .OrderBy(o => o.Period.Start);
    }
}
