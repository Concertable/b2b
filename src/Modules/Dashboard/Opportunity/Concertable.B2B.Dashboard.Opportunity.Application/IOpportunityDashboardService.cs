using Reunion;

namespace Concertable.B2B.Dashboard.Opportunity.Application;

internal interface IOpportunityDashboardService
{
    Task<Result<IReadOnlyList<OpportunityApplicationMetrics>, OpportunityDashboardError>>
        GetOpenAsync(CancellationToken ct = default);

    Task<Result<IReadOnlyList<OpportunityMatch>, OpportunityDashboardError>>
        GetRecommendedAsync(CancellationToken ct = default);
}
