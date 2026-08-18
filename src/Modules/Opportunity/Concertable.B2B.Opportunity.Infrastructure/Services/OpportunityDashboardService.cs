namespace Concertable.B2B.Opportunity.Infrastructure.Services;

internal sealed class OpportunityDashboardService : IOpportunityDashboardService
{
    private readonly IOpportunityReadRepository repository;

    public OpportunityDashboardService(IOpportunityReadRepository repository) =>
        this.repository = repository;

    public Task<IReadOnlySet<int>> GetUpcomingIdsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default) =>
        repository.GetUpcomingIdsAsync(opportunityIds, ct);

    public Task<int> GetOpenCountAsync(
        Guid venueTenantId,
        CancellationToken ct = default) =>
        repository.GetOpenCountAsync(venueTenantId, ct);
}
