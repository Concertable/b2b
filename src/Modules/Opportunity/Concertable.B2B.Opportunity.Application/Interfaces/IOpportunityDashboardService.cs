namespace Concertable.B2B.Opportunity.Application.Interfaces;

internal interface IOpportunityDashboardService
{
    Task<IReadOnlySet<int>> GetUpcomingIdsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default);
    Task<int> GetOpenCountAsync(
        Guid venueTenantId,
        CancellationToken ct = default);
}
