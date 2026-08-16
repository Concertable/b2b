using Reunion;

namespace Concertable.B2B.Concert.Contracts;

public interface IConcertModule
{
    Task<Option<VenueDashboardCounts>> GetVenueDashboardCountsAsync(int venueId, CancellationToken ct = default);
    Task<Option<ArtistDashboardCounts>> GetArtistDashboardCountsAsync(int artistId, CancellationToken ct = default);
    Task<IReadOnlyList<ManagerSettlementContext>> GetManagerSettlementContextsAsync(
        IReadOnlyCollection<int> bookingIds,
        CancellationToken ct = default);
}
