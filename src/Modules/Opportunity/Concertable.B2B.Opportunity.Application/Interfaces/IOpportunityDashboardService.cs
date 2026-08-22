using Concertable.B2B.Opportunity.Application.DTOs;
using Concertable.B2B.Opportunity.Application.Errors;

namespace Concertable.B2B.Opportunity.Application.Interfaces;

internal interface IOpportunityDashboardService
{
    Task<IReadOnlySet<int>> GetUpcomingIdsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default);
    Task<int> GetOpenCountAsync(
        Guid venueTenantId,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<OpportunityApplicationMetrics>, OpportunityError>>
        GetApplicationMetricsForCurrentVenueAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<OpportunityMatch>, OpportunityError>>
        GetMatchesForCurrentArtistAsync(CancellationToken ct = default);
}
