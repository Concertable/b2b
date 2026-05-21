using Concertable.Concert.Contracts;

namespace Concertable.Concert.Application.Interfaces;

internal interface IConcertDashboardRepository
{
    Task<VenueDashboardCountsDto?> GetVenueCountsAsync(int venueId, CancellationToken ct = default);
    Task<ArtistDashboardCountsDto?> GetArtistCountsAsync(int artistId, CancellationToken ct = default);
}
