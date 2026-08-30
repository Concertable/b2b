namespace Concertable.B2B.Opportunity.Infrastructure;

internal sealed class OpportunityModule : IOpportunityModule
{
    private readonly IOpportunityService service;

    public OpportunityModule(IOpportunityService service)
    {
        this.service = service;
    }

    public Task<Option<OpportunityDto>> GetAsync(
        int opportunityId,
        CancellationToken ct = default) =>
        service.GetAsync(opportunityId, ct);

    public Task<IReadOnlyList<OpportunityDto>> GetAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default) =>
        service.GetAsync(opportunityIds, ct);

    public Task<Option<OpportunityDto>> GetOpenAsync(
        int opportunityId,
        CancellationToken ct = default) =>
        service.GetOpenAsync(opportunityId, ct);

    public Task<IReadOnlySet<int>> GetUpcomingIdsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default) =>
        service.GetUpcomingIdsAsync(opportunityIds, ct);

    public Task<int> GetOpenCountAsync(
        Guid venueTenantId,
        CancellationToken ct = default) =>
        service.GetOpenCountAsync(venueTenantId, ct);

    public Task<IReadOnlyList<OpportunityDto>> GetOpenByVenueTenantIdAsync(
        Guid venueTenantId,
        CancellationToken ct = default) =>
        service.GetOpenByVenueTenantIdAsync(venueTenantId, ct);

    public Task<IReadOnlyList<OpportunityDto>> GetRecommendedAsync(
        IReadOnlyCollection<int> excludedOpportunityIds,
        IReadOnlySet<Genre> genres,
        CancellationToken ct = default) =>
        service.GetRecommendedAsync(excludedOpportunityIds, genres, ct);
}
