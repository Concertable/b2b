using Concertable.B2B.Artist.Application.DTOs;
using Concertable.B2B.Artist.Application.Interfaces;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;

namespace Concertable.B2B.Artist.Infrastructure.Services;

internal sealed class ArtistDashboardService : IArtistDashboardService
{
    private readonly IConcertModule concertModule;
    private readonly IManagerPaymentReportingClient paymentReportingClient;
    private readonly ITenantContext tenantContext;
    private readonly TimeProvider timeProvider;

    public ArtistDashboardService(
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

    public async Task<Option<ArtistDashboardKpis>> GetKpisAsync(CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var counts = await concertModule.GetArtistDashboardCountsAsync(tenantId, ct);
        if (!counts.TryGetValue(out var dashboardCounts))
            return null;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var mtdPayouts = now == monthStart
            ? Money.Gbp(0m)
            : await paymentReportingClient.GetSettlementPayoutsAsync(
                tenantId,
                new DateRange(monthStart, now),
                ct);

        return new ArtistDashboardKpis(
            PendingApplications: dashboardCounts.PendingApplications,
            AcceptedAwaitingCheckout: dashboardCounts.AcceptedAwaitingCheckout,
            UpcomingConcerts: dashboardCounts.UpcomingConcerts,
            MtdPayoutsCents: mtdPayouts.ToMinorUnits(),
            MtdPayoutsDeltaPercent: null);
    }
}
