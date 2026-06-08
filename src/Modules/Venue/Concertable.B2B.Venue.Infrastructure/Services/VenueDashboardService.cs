using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Venue.Application.DTOs;
using Concertable.B2B.Venue.Application.Interfaces;

namespace Concertable.B2B.Venue.Infrastructure.Services;

internal sealed class VenueDashboardService : IVenueDashboardService
{
    private readonly IVenueService venueService;
    private readonly IConcertModule concertModule;

    public VenueDashboardService(IVenueService venueService, IConcertModule concertModule)
    {
        this.venueService = venueService;
        this.concertModule = concertModule;
    }

    public async Task<VenueDashboardKpis?> GetKpisAsync(CancellationToken ct = default)
    {
        var venueId = await venueService.GetIdForCurrentUserAsync();

        var countsTask = concertModule.GetVenueDashboardCountsAsync(venueId, ct);
        // TODO B.11: var mtdRevenueTask = paymentModule.GetVenueTicketRevenueMtdAsync(venueId, ct);
        await Task.WhenAll(countsTask);

        var counts = countsTask.Result;
        if (counts is null) return null;

        return new VenueDashboardKpis(
            ApplicationsToReview: counts.ApplicationsToReview,
            ApplicationsToReviewDelta: null,
            OpenOpportunities: counts.OpenOpportunities,
            UpcomingConcerts: counts.UpcomingConcerts,
            MtdRevenueCents: 0,
            MtdRevenueDeltaPercent: null);
    }
}
