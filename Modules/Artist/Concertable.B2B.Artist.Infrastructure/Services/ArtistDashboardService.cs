using Concertable.B2B.Artist.Application.DTOs;
using Concertable.B2B.Artist.Application.Interfaces;
using Concertable.B2B.Concert.Contracts;

namespace Concertable.B2B.Artist.Infrastructure.Services;

internal sealed class ArtistDashboardService : IArtistDashboardService
{
    private readonly IArtistService artistService;
    private readonly IConcertModule concertModule;

    public ArtistDashboardService(IArtistService artistService, IConcertModule concertModule)
    {
        this.artistService = artistService;
        this.concertModule = concertModule;
    }

    public async Task<ArtistDashboardKpisDto?> GetKpisAsync(CancellationToken ct = default)
    {
        var artistId = await artistService.GetIdForCurrentUserAsync();

        var countsTask = concertModule.GetArtistDashboardCountsAsync(artistId, ct);
        // TODO B.11: var mtdPayoutsTask = paymentModule.GetArtistPayoutsMtdAsync(artistId, ct);
        await Task.WhenAll(countsTask);

        var counts = countsTask.Result;
        if (counts is null) return null;

        return new ArtistDashboardKpisDto(
            PendingApplications: counts.PendingApplications,
            AcceptedAwaitingCheckout: 0, // TODO B.11: IConcertWorkflowCapabilityRegistry / IAcceptsCheckout
            UpcomingConcerts: counts.UpcomingConcerts,
            MtdPayoutsCents: 0,
            MtdPayoutsDeltaPercent: null);
    }
}
