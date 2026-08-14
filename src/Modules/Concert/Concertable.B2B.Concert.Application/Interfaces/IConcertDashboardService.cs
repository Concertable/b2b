using Concertable.B2B.Concert.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertDashboardService
{
    Task<VenueDashboardCounts?> GetVenueCountsAsync(int venueId, CancellationToken ct = default);
    Task<ArtistDashboardCounts?> GetArtistCountsAsync(int artistId, CancellationToken ct = default);
    Task<IReadOnlyList<ManagerSettlementContext>> GetManagerSettlementContextsAsync(
        IReadOnlyCollection<int> bookingIds,
        CancellationToken ct = default);
}
