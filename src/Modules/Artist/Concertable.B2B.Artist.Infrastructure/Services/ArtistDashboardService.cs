using Concertable.B2B.Artist.Application.DTOs;
using Concertable.B2B.Artist.Application.Interfaces;
using Concertable.B2B.Artist.Infrastructure.Extensions;
using Concertable.B2B.Artist.Infrastructure.Mappers;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;

namespace Concertable.B2B.Artist.Infrastructure.Services;

internal sealed class ArtistDashboardService : IArtistDashboardService
{
    private readonly IArtistService artistService;
    private readonly IConcertModule concertModule;
    private readonly IArtistReviewService reviewService;
    private readonly IManagerPaymentReportingClient paymentReportingClient;
    private readonly IPayoutAccountOperationsClient payoutAccountClient;
    private readonly ITenantContext tenantContext;
    private readonly ITenantModule tenantModule;
    private readonly TimeProvider timeProvider;

    public ArtistDashboardService(
        IArtistService artistService,
        IConcertModule concertModule,
        IArtistReviewService reviewService,
        IManagerPaymentReportingClient paymentReportingClient,
        IPayoutAccountOperationsClient payoutAccountClient,
        ITenantContext tenantContext,
        ITenantModule tenantModule,
        TimeProvider timeProvider)
    {
        this.artistService = artistService;
        this.concertModule = concertModule;
        this.reviewService = reviewService;
        this.paymentReportingClient = paymentReportingClient;
        this.payoutAccountClient = payoutAccountClient;
        this.tenantContext = tenantContext;
        this.tenantModule = tenantModule;
        this.timeProvider = timeProvider;
    }

    public async Task<ArtistDashboardOverview?> GetOverviewAsync(CancellationToken ct = default)
    {
        var artist = await artistService.GetDetailsForCurrentUserAsync();
        if (artist is null)
            return null;

        var tenantId = tenantContext.GetTenantId();
        var reviewSummaryTask = reviewService.GetSummaryAsync(artist.Id, ct);
        var payoutStatusTask = payoutAccountClient.GetAccountStatusAsync(tenantId, ct);
        await Task.WhenAll(reviewSummaryTask, payoutStatusTask);

        var reviewSummary = await reviewSummaryTask;
        var payoutStatus = await payoutStatusTask;
        return new ArtistDashboardOverview(
            artist.Id,
            artist.Name,
            artist.ToProfileHealth(payoutStatus),
            payoutStatus.ToStripeConnectStatus(),
            reviewSummary);
    }

    public async Task<ArtistDashboardKpis?> GetKpisAsync(CancellationToken ct = default)
    {
        var artistId = await artistService.GetIdForCurrentUserAsync();
        var tenantId = tenantContext.GetTenantId();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var monthStart = now.StartOfMonth();

        var countsTask = concertModule.GetArtistDashboardCountsAsync(artistId, ct);
        var mtdPayoutsTask = now == monthStart
            ? Task.FromResult(Money.Gbp(0m))
            : paymentReportingClient.GetSettlementPayoutsAsync(
                tenantId,
                new DateRange(monthStart, now),
                ct);
        await Task.WhenAll(countsTask, mtdPayoutsTask);

        var counts = await countsTask;
        if (counts is null) return null;
        var mtdPayouts = await mtdPayoutsTask;

        return new ArtistDashboardKpis(
            PendingApplications: counts.PendingApplications,
            AcceptedAwaitingCheckout: counts.AcceptedAwaitingCheckout,
            UpcomingConcerts: counts.UpcomingConcerts,
            MtdPayoutsCents: mtdPayouts.ToMinorUnits());
    }

    public async Task<IReadOnlyList<MonthlyRevenuePoint>> GetPayoutsAsync(CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var firstMonth = now.StartOfMonth().AddMonths(-5);
        var points = await paymentReportingClient.GetSettlementPayoutsByMonthAsync(
            tenantId,
            new DateRange(firstMonth, now),
            ct);

        return points.ToMonthlyRevenuePoints(firstMonth);
    }

    public Task<IReadOnlyList<ActivityItemDto>> GetActivityAsync(CancellationToken ct = default) =>
        tenantModule.GetRecentActivityAsync(tenantContext.GetTenantId(), 10, ct);
}
