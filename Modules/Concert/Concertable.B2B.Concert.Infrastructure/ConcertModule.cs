using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Contracts;

namespace Concertable.B2B.Concert.Infrastructure;

internal sealed class ConcertModule(IConcertDashboardRepository dashboardRepository) : IConcertModule
{
    public Task<VenueDashboardCountsDto?> GetVenueDashboardCountsAsync(int venueId, CancellationToken ct = default) =>
        dashboardRepository.GetVenueCountsAsync(venueId, ct);

    public Task<ArtistDashboardCountsDto?> GetArtistDashboardCountsAsync(int artistId, CancellationToken ct = default) =>
        dashboardRepository.GetArtistCountsAsync(artistId, ct);
}
