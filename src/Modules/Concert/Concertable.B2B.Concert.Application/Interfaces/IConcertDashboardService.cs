using Concertable.B2B.Concert.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertDashboardService
{
    Task<Option<VenueDashboardCounts>> GetVenueCountsAsync(
        Guid venueTenantId,
        CancellationToken ct = default);
    Task<Option<ArtistDashboardCounts>> GetArtistCountsAsync(
        Guid artistTenantId,
        CancellationToken ct = default);
    Task<IReadOnlyList<ManagerSettlementContext>> GetManagerSettlementContextsAsync(
        IReadOnlyCollection<int> bookingIds,
        CancellationToken ct = default);
}
