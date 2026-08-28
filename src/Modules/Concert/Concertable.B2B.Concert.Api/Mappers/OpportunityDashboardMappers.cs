using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Application.DTOs;

namespace Concertable.B2B.Concert.Api.Mappers;

internal static class OpportunityDashboardMappers
{
    extension(IEnumerable<OpportunityApplicationMetrics> metrics)
    {
        public IReadOnlyList<OpportunityApplicationMetricsResponse> ToApplicationMetricsResponses() =>
            metrics.Select(item => item.ToResponse()).ToList();
    }

    extension(OpportunityApplicationMetrics metrics)
    {
        private OpportunityApplicationMetricsResponse ToResponse() => new(
            new OpportunitySummaryResponse(
                metrics.Opportunity.Id,
                metrics.Opportunity.VenueId,
                metrics.Opportunity.VenueName,
                metrics.Opportunity.StartDate,
                metrics.Opportunity.EndDate,
                metrics.Opportunity.Genres,
                metrics.Opportunity.Deal),
            metrics.ApplicationCount,
            metrics.DaysUntilDeadline);
    }

    extension(IEnumerable<OpportunityMatch> matches)
    {
        public IReadOnlyList<OpportunityMatchResponse> ToMatchResponses() =>
            matches.Select(match => match.ToResponse()).ToList();
    }

    extension(OpportunityMatch match)
    {
        private OpportunityMatchResponse ToResponse() => new(
            match.Opportunity.Id,
            match.Opportunity.VenueId,
            match.Opportunity.VenueName,
            match.County,
            match.Town,
            match.Opportunity.StartDate,
            match.Opportunity.EndDate,
            match.Opportunity.Genres,
            match.Opportunity.Deal,
            match.FitScore,
            $"/_artist/find/venue/{match.Opportunity.VenueId}");
    }
}
