using Concertable.B2B.Application.Contracts;
namespace Concertable.B2B.Application.Infrastructure;

internal sealed class ApplicationModule : IApplicationModule
{
    private readonly IApplicationDashboardService dashboardService;

    public ApplicationModule(IApplicationDashboardService dashboardService) =>
        this.dashboardService = dashboardService;

    public Task<int> GetVenuePendingCountAsync(
        Guid venueTenantId,
        CancellationToken ct = default) =>
        dashboardService.GetVenuePendingCountAsync(venueTenantId, ct);

    public Task<int> GetArtistPendingCountAsync(
        Guid artistTenantId,
        CancellationToken ct = default) =>
        dashboardService.GetArtistPendingCountAsync(artistTenantId, ct);

    public Task<IReadOnlyDictionary<int, int>> GetCountsByOpportunityIdsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default) =>
        dashboardService.GetCountsByOpportunityIdsAsync(opportunityIds, ct);

    public Task<IReadOnlySet<int>> GetOpportunityIdsForArtistTenantAsync(
        Guid artistTenantId,
        CancellationToken ct = default) =>
        dashboardService.GetOpportunityIdsForArtistTenantAsync(artistTenantId, ct);

}
