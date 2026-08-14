namespace Concertable.B2B.Concert.Contracts;

public interface IConcertModule
{
    Task<VenueDashboardCounts?> GetVenueDashboardCountsAsync(int venueId, CancellationToken ct = default);
    Task<ArtistDashboardCounts?> GetArtistDashboardCountsAsync(int artistId, CancellationToken ct = default);
    Task<IReadOnlyList<ManagerSettlementContext>> GetManagerSettlementContextsAsync(
        IReadOnlyCollection<int> bookingIds,
        CancellationToken ct = default);
}
