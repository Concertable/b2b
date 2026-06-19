using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertDashboardService : IConcertDashboardService
{
    private readonly IConcertDashboardRepository repository;

    public ConcertDashboardService(IConcertDashboardRepository repository)
    {
        this.repository = repository;
    }

    public Task<VenueDashboardCounts?> GetVenueCountsAsync(int venueId, CancellationToken ct = default) =>
        repository.GetVenueCountsAsync(venueId, ct);

    public Task<ArtistDashboardCounts?> GetArtistCountsAsync(int artistId, CancellationToken ct = default) =>
        repository.GetArtistCountsAsync(artistId, ct);
}
