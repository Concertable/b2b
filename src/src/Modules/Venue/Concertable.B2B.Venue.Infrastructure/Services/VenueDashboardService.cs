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
    private readonly IConcertModule concertModule;
    private readonly IManagerPaymentReportingClient paymentReportingClient;
    private readonly ITenantContext tenantContext;
    private readonly TimeProvider timeProvider;

    public VenueDashboardService(
        IConcertModule concertModule,
        IManagerPaymentReportingClient paymentReportingClient,
        ITenantContext tenantContext,
        TimeProvider timeProvider)
    {
        this.concertModule = concertModule;
        this.paymentReportingClient = paymentReportingClient;
        this.tenantContext = tenantContext;
        this.timeProvider = timeProvider;
    }

    public async Task<Option<VenueDashboardKpis>> GetKpisAsync(CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var counts = await concertModule.GetVenueDashboardCountsAsync(tenantId, ct);
        if (!counts.TryGetValue(out var dashboardCounts))
            return null;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var mtdRevenue = now == monthStart
            ? Money.Gbp(0m)
            : await paymentReportingClient.GetTicketRevenueAsync(
                tenantId,
                new DateRange(monthStart, now),
                ct);

        return new VenueDashboardKpis(
            ApplicationsToReview: dashboardCounts.ApplicationsToReview,
            ApplicationsToReviewDelta: null,
            OpenOpportunities: dashboardCounts.OpenOpportunities,
            UpcomingConcerts: dashboardCounts.UpcomingConcerts,
            AwaitingDoorRevenue: dashboardCounts.AwaitingDoorRevenue,
            MtdRevenueCents: mtdRevenue.ToMinorUnits(),
            MtdRevenueDeltaPercent: null);
    }
}
