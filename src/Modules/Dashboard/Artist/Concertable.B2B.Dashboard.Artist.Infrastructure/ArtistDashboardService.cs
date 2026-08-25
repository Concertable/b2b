using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Dashboard.Artist.Application;
using Concertable.B2B.Dashboard.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;
using Reunion;

namespace Concertable.B2B.Dashboard.Artist.Infrastructure;

internal sealed class ArtistDashboardService : IArtistDashboardService
{
    private readonly IApplicationModule applications;
    private readonly IArtistModule artists;
    private readonly IBookingModule bookings;
    private readonly IConcertModule concerts;
    private readonly IManagerPaymentReportingClient paymentReportingClient;
    private readonly IPayoutAccountOperationsClient payoutAccountClient;
    private readonly ITenantContext tenantContext;
    private readonly ITenantModule tenants;
    private readonly TimeProvider timeProvider;

    public ArtistDashboardService(
        IApplicationModule applications,
        IArtistModule artists,
        IBookingModule bookings,
        IConcertModule concerts,
        IManagerPaymentReportingClient paymentReportingClient,
        IPayoutAccountOperationsClient payoutAccountClient,
        ITenantContext tenantContext,
        ITenantModule tenants,
        TimeProvider timeProvider)
    {
        this.applications = applications;
        this.artists = artists;
        this.bookings = bookings;
        this.concerts = concerts;
        this.paymentReportingClient = paymentReportingClient;
        this.payoutAccountClient = payoutAccountClient;
        this.tenantContext = tenantContext;
        this.tenants = tenants;
        this.timeProvider = timeProvider;
    }

    public async Task<Option<ArtistDashboardKpis>> GetAsync(CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var period = DashboardReportingPeriod.From(timeProvider.GetUtcNow().UtcDateTime);
        var pendingTask = applications.GetArtistPendingCountAsync(tenantId, ct);
        var awaitingCheckoutTask = bookings.GetArtistAwaitingCheckoutCountAsync(tenantId, ct);
        var concertCountsTask = concerts.GetArtistDashboardCountsAsync(tenantId, ct);
        var payoutsTask = period.HasElapsedTime
            ? paymentReportingClient.GetSettlementPayoutsAsync(
                tenantId,
                new DateRange(period.MonthStart, period.Now),
                ct)
            : Task.FromResult(Money.Gbp(0m));

        await Task.WhenAll(pendingTask, awaitingCheckoutTask, concertCountsTask, payoutsTask);
        var concertCounts = await concertCountsTask;
        if (!concertCounts.TryGetValue(out var counts))
            return null;

        return new ArtistDashboardKpis(
            await pendingTask,
            await awaitingCheckoutTask,
            counts.UpcomingConcerts,
            (await payoutsTask).ToMinorUnits(),
            null);
    }

    public async Task<Option<ArtistDashboardOverview>> GetOverviewAsync(
        CancellationToken ct = default) =>
        await (await artists.GetCurrentProfileAsync(ct))
            .MapAsync(async artist =>
            {
                var tenantId = tenantContext.GetTenantId();
                var reviewSummaryTask = artists.GetReviewSummaryAsync(artist.Id, ct);
                var payoutStatusTask = payoutAccountClient.GetAccountStatusAsync(tenantId, ct);
                await Task.WhenAll(reviewSummaryTask, payoutStatusTask);

                var payoutStatus = await payoutStatusTask;
                return new ArtistDashboardOverview(
                    artist.Id,
                    artist.Name,
                    artist.ToProfileHealth(payoutStatus),
                    payoutStatus.ToStripeConnectStatus(),
                    await reviewSummaryTask);
            });

    public async Task<IReadOnlyList<MonthlyRevenuePoint>> GetPayoutsAsync(
        CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var firstMonth = DashboardReportingPeriod.From(now).MonthStart.AddMonths(-5);
        var points = await paymentReportingClient.GetSettlementPayoutsByMonthAsync(
            tenantId,
            new DateRange(firstMonth, now),
            ct);

        return points.ToMonthlyRevenuePoints(firstMonth);
    }

    public Task<IReadOnlyList<ActivityItemDto>> GetActivityAsync(
        CancellationToken ct = default) =>
        tenants.GetRecentActivityAsync(tenantContext.GetTenantId(), 10, ct);
}
