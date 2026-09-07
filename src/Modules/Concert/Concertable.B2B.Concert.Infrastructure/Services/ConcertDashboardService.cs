using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertDashboardService : IConcertDashboardService
{
    private readonly IConcertDashboardRepository repository;

    public ConcertDashboardService(IConcertDashboardRepository repository)
    {
        this.repository = repository;
    }

    public async Task<Option<VenueDashboardCounts>> GetVenueCountsAsync(
        Guid venueTenantId,
        CancellationToken ct = default)
    {
        var concertCounts = await repository.GetVenueCountsAsync(venueTenantId, ct);
        if (concertCounts is null)
            return Option.None<VenueDashboardCounts>();

        return Option.Some(new VenueDashboardCounts(
            concertCounts.UpcomingConcerts,
            concertCounts.AwaitingDoorRevenue));
    }

    public async Task<Option<ArtistDashboardCounts>> GetArtistCountsAsync(
        Guid artistTenantId,
        CancellationToken ct = default)
    {
        var concertCounts = await repository.GetArtistCountsAsync(artistTenantId, ct);
        if (concertCounts is null)
            return Option.None<ArtistDashboardCounts>();

        return Option.Some(new ArtistDashboardCounts(concertCounts.UpcomingConcerts));
    }

    public Task<IReadOnlyList<SettlementContext>> GetSettlementContextsAsync(
        IReadOnlyCollection<int> concertIds,
        CancellationToken ct = default) =>
        repository.GetSettlementContextsAsync(concertIds, ct);
}
