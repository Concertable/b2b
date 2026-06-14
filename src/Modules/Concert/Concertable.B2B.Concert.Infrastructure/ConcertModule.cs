using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Contracts;

namespace Concertable.B2B.Concert.Infrastructure;

internal sealed class ConcertModule : IConcertModule
{
    private readonly IConcertDashboardService dashboardService;

    public ConcertModule(IConcertDashboardService dashboardService)
    {
        this.dashboardService = dashboardService;
    }

    public Task<VenueDashboardCounts?> GetVenueDashboardCountsAsync(int venueId, CancellationToken ct = default) =>
        dashboardService.GetVenueCountsAsync(venueId, ct);

    public Task<ArtistDashboardCounts?> GetArtistDashboardCountsAsync(int artistId, CancellationToken ct = default) =>
        dashboardService.GetArtistCountsAsync(artistId, ct);
}
