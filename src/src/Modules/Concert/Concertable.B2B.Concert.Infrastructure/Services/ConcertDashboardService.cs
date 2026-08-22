using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Capabilities;
using Concertable.B2B.Concert.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertDashboardService : IConcertDashboardService
{
    private readonly IConcertDashboardRepository repository;
    private readonly IConcertWorkflowCapabilityRegistry capabilityRegistry;

    public ConcertDashboardService(
        IConcertDashboardRepository repository,
        IConcertWorkflowCapabilityRegistry capabilityRegistry)
    {
        this.repository = repository;
        this.capabilityRegistry = capabilityRegistry;
    }

    public async Task<Option<VenueDashboardCounts>> GetVenueCountsAsync(
        Guid venueTenantId,
        CancellationToken ct = default) =>
        await repository.GetVenueCountsAsync(venueTenantId, ct);

    public async Task<Option<ArtistDashboardCounts>> GetArtistCountsAsync(
        Guid artistTenantId,
        CancellationToken ct = default) =>
        await repository.GetArtistCountsAsync(
            artistTenantId,
            capabilityRegistry.DealTypesWith<IAcceptsCheckout>(),
            ct);

    public Task<IReadOnlyList<ManagerSettlementContext>> GetManagerSettlementContextsAsync(
        IReadOnlyCollection<int> bookingIds,
        CancellationToken ct = default) =>
        repository.GetManagerSettlementContextsAsync(bookingIds, ct);
}
