using Concertable.B2B.Concert.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertDashboardRepository
{
    Task<VenueDashboardCountsDto?> GetVenueCountsAsync(int venueId, CancellationToken ct = default);
    Task<ArtistDashboardCountsDto?> GetArtistCountsAsync(int artistId, CancellationToken ct = default);
}
