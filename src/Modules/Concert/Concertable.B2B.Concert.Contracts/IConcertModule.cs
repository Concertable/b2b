namespace Concertable.B2B.Concert.Contracts;

public interface IConcertModule
{
    Task<VenueDashboardCountsDto?> GetVenueDashboardCountsAsync(int venueId, CancellationToken ct = default);
    Task<ArtistDashboardCountsDto?> GetArtistDashboardCountsAsync(int artistId, CancellationToken ct = default);
}
