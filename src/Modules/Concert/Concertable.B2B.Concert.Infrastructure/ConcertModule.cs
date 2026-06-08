using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Contracts;

namespace Concertable.B2B.Concert.Infrastructure;

internal sealed class ConcertModule : IConcertModule
{
    private readonly IConcertDashboardRepository dashboardRepository;

    public ConcertModule(IConcertDashboardRepository dashboardRepository)
    {
        this.dashboardRepository = dashboardRepository;
    }

    public Task<VenueDashboardCounts?> GetVenueDashboardCountsAsync(int venueId, CancellationToken ct = default) =>
        dashboardRepository.GetVenueCountsAsync(venueId, ct);

    public Task<ArtistDashboardCounts?> GetArtistDashboardCountsAsync(int artistId, CancellationToken ct = default) =>
        dashboardRepository.GetArtistCountsAsync(artistId, ct);
}
