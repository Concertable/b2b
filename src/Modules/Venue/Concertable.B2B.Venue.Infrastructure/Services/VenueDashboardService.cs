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
        var countsOption = await concertModule.GetVenueDashboardCountsAsync(tenantId, ct);
        if (!countsOption.TryGetValue(out var counts))
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
            ApplicationsToReview: counts.ApplicationsToReview,
            ApplicationsToReviewDelta: null,
            OpenOpportunities: counts.OpenOpportunities,
            UpcomingConcerts: counts.UpcomingConcerts,
            AwaitingDoorRevenue: counts.AwaitingDoorRevenue,
            MtdRevenueCents: mtdRevenue.ToMinorUnits(),
            MtdRevenueDeltaPercent: null);
    }
}
