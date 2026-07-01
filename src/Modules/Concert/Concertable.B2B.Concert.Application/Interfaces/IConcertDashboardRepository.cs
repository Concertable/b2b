using Concertable.B2B.Concert.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertDashboardRepository
{
    Task<VenueDashboardCounts?> GetVenueCountsAsync(int venueId, CancellationToken ct = default);
    Task<ArtistDashboardCounts?> GetArtistCountsAsync(int artistId, CancellationToken ct = default);
}
