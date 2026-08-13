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

    public Task<VenueDashboardCounts?> GetVenueCountsAsync(int venueId, CancellationToken ct = default) =>
        repository.GetVenueCountsAsync(venueId, ct);

    public Task<ArtistDashboardCounts?> GetArtistCountsAsync(int artistId, CancellationToken ct = default) =>
        repository.GetArtistCountsAsync(
            artistId,
            capabilityRegistry.DealTypesWith<IAcceptsCheckout>(),
            ct);
}
