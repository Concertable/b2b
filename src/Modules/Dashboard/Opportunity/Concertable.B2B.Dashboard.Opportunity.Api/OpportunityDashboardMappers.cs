using Concertable.B2B.Dashboard.Opportunity.Application;

namespace Concertable.B2B.Dashboard.Opportunity.Api;

internal static class OpportunityDashboardMappers
{
    extension(IReadOnlyList<OpportunityApplicationMetrics> metrics)
    {
        public IReadOnlyList<OpportunityApplicationMetricsResponse> ToResponses() =>
            metrics.Select(item => new OpportunityApplicationMetricsResponse(
                    item.Opportunity.ToResponse(),
                    item.ApplicationCount,
                    item.DaysUntilDeadline))
                .ToList();
    }

    extension(IReadOnlyList<OpportunityMatch> matches)
    {
        public IReadOnlyList<OpportunityMatchResponse> ToResponses() =>
            matches.Select(match => new OpportunityMatchResponse(
                    match.Opportunity.Id,
                    match.Opportunity.VenueId,
                    match.Opportunity.VenueName,
                    match.County,
                    match.Town,
                    match.Opportunity.StartDate,
                    match.Opportunity.EndDate,
                    match.Opportunity.Genres.ToList(),
                    match.Opportunity.Deal,
                    match.FitScore,
                    $"/_artist/find/venue/{match.Opportunity.VenueId}"))
                .ToList();
    }

    extension(OpportunitySummary summary)
    {
        private OpportunitySummaryResponse ToResponse() =>
            new(
                summary.Id,
                summary.VenueId,
                summary.VenueName,
                summary.StartDate,
                summary.EndDate,
                summary.Genres.ToList(),
                summary.Deal);
    }
}
