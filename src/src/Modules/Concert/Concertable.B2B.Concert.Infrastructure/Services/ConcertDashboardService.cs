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

    public async Task<Option<VenueDashboardCounts>> GetVenueCountsAsync(int venueId, CancellationToken ct = default) =>
        (await repository.GetVenueCountsAsync(venueId, ct)).ToOption();

    public async Task<Option<ArtistDashboardCounts>> GetArtistCountsAsync(int artistId, CancellationToken ct = default) =>
        (await repository.GetArtistCountsAsync(artistId, ct)).ToOption();
}
