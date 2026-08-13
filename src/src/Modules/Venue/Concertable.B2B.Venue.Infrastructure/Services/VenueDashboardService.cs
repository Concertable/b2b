using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Venue.Application.DTOs;
using Concertable.B2B.Venue.Application.Interfaces;
using Concertable.Kernel.Identity;
using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;

namespace Concertable.B2B.Venue.Infrastructure.Services;

internal sealed class VenueDashboardService : IVenueDashboardService
{
    private readonly IVenueService venueService;
    private readonly IConcertModule concertModule;
    private readonly IManagerPaymentReportingClient paymentReportingClient;
    private readonly ITenantContext tenantContext;
    private readonly TimeProvider timeProvider;

    public VenueDashboardService(
        IVenueService venueService,
        IConcertModule concertModule,
        IManagerPaymentReportingClient paymentReportingClient,
        ITenantContext tenantContext,
        TimeProvider timeProvider)
    {
        this.venueService = venueService;
        this.concertModule = concertModule;
        this.paymentReportingClient = paymentReportingClient;
        this.tenantContext = tenantContext;
        this.timeProvider = timeProvider;
    }

    public async Task<VenueDashboardKpis?> GetKpisAsync(CancellationToken ct = default)
    {
        var venueId = await venueService.GetIdForCurrentUserAsync();
        var tenantId = tenantContext.GetTenantId();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var countsTask = concertModule.GetVenueDashboardCountsAsync(venueId, ct);
        var mtdRevenueTask = now == monthStart
            ? Task.FromResult(Money.Gbp(0m))
            : paymentReportingClient.GetTicketRevenueAsync(
                tenantId,
                new DateRange(monthStart, now),
                ct);
        await Task.WhenAll(countsTask, mtdRevenueTask);

        var counts = countsTask.Result;
        if (counts is null) return null;

        return new VenueDashboardKpis(
            ApplicationsToReview: counts.ApplicationsToReview,
            ApplicationsToReviewDelta: null,
            OpenOpportunities: counts.OpenOpportunities,
            UpcomingConcerts: counts.UpcomingConcerts,
            AwaitingDoorRevenue: counts.AwaitingDoorRevenue,
            MtdRevenueCents: mtdRevenueTask.Result.ToMinorUnits(),
            MtdRevenueDeltaPercent: null);
    }
}
