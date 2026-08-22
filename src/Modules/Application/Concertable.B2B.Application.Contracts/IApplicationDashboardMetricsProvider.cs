namespace Concertable.B2B.Application.Contracts;

public interface IApplicationDashboardMetricsProvider
{
    Task<IReadOnlyDictionary<int, int>> GetCountsByOpportunityIdsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default);

    Task<IReadOnlySet<int>> GetOpportunityIdsForArtistTenantAsync(
        Guid artistTenantId,
        CancellationToken ct = default);
}
