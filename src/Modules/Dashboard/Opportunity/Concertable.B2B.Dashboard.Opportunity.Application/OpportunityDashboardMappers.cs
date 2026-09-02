using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Venue.Contracts;

namespace Concertable.B2B.Dashboard.Opportunity.Application;

internal static class OpportunityDashboardMappers
{
    extension(OpportunityDto opportunity)
    {
        public OpportunitySummary ToSummary(
            IReadOnlyDictionary<int, DealDto> dealsById,
            IReadOnlyDictionary<int, VenueDto> venuesById) =>
            new(
                opportunity.Id,
                opportunity.VenueId,
                venuesById[opportunity.VenueId].Name,
                opportunity.StartDate,
                opportunity.EndDate,
                opportunity.Genres,
                dealsById[opportunity.DealId]);

        public OpportunityMatch ToMatch(
            IReadOnlyDictionary<int, DealDto> dealsById,
            IReadOnlyDictionary<int, VenueDto> venuesById,
            int fitScore)
        {
            var venue = venuesById[opportunity.VenueId];
            return new OpportunityMatch(
                opportunity.ToSummary(dealsById, venuesById),
                venue.County,
                venue.Town,
                fitScore);
        }
    }
}
