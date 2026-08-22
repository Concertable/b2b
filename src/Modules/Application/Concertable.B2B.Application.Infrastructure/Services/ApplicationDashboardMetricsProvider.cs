using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Application.Contracts;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ApplicationDashboardMetricsProvider(
    IApplicationRepository repository) : IApplicationDashboardMetricsProvider
{
    public Task<IReadOnlyDictionary<int, int>> GetCountsByOpportunityIdsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default) =>
        repository.GetCountsByOpportunityIdsAsync(opportunityIds, ct);

    public Task<IReadOnlySet<int>> GetOpportunityIdsForArtistTenantAsync(
        Guid artistTenantId,
        CancellationToken ct = default) =>
        repository.GetOpportunityIdsForArtistTenantAsync(artistTenantId, ct);
}
