using Concertable.B2B.Artist.Application.DTOs;
using Concertable.B2B.Artist.Application.Interfaces;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;
using PaymentPayoutAccountStatus = Concertable.Payment.Client.Enums.PayoutAccountStatus;

namespace Concertable.B2B.Artist.Infrastructure.Services;

internal sealed class ArtistDashboardService : IArtistDashboardService
{
    private readonly IArtistService artistService;
    private readonly IConcertModule concertModule;
    private readonly IArtistReviewService reviewService;
    private readonly IManagerPaymentReportingClient paymentReportingClient;
    private readonly IPayoutAccountOperationsClient payoutAccountClient;
    private readonly ITenantContext tenantContext;
    private readonly TimeProvider timeProvider;

    public ArtistDashboardService(
        IArtistService artistService,
        IConcertModule concertModule,
        IArtistReviewService reviewService,
        IManagerPaymentReportingClient paymentReportingClient,
        IPayoutAccountOperationsClient payoutAccountClient,
        ITenantContext tenantContext,
        TimeProvider timeProvider)
    {
        this.artistService = artistService;
        this.concertModule = concertModule;
        this.reviewService = reviewService;
        this.paymentReportingClient = paymentReportingClient;
        this.payoutAccountClient = payoutAccountClient;
        this.tenantContext = tenantContext;
        this.timeProvider = timeProvider;
    }

    public async Task<ArtistDashboardOverview?> GetOverviewAsync(CancellationToken ct = default)
    {
        var artist = await artistService.GetDetailsForCurrentUserAsync();
        if (artist is null)
            return null;

        var tenantId = tenantContext.GetTenantId();
        var reviewSummaryTask = reviewService.GetSummaryAsync(artist.Id);
        var payoutStatusTask = payoutAccountClient.GetAccountStatusAsync(tenantId, ct);
        await Task.WhenAll(reviewSummaryTask, payoutStatusTask);

        var profileHealth = ToProfileHealth(artist, payoutStatusTask.Result);
        return new ArtistDashboardOverview(
            artist.Id,
            artist.Name,
            profileHealth,
            ToStripeConnectStatus(payoutStatusTask.Result),
            reviewSummaryTask.Result);
    }

    public async Task<ArtistDashboardKpis?> GetKpisAsync(CancellationToken ct = default)
    {
        var artistId = await artistService.GetIdForCurrentUserAsync();
        var tenantId = tenantContext.GetTenantId();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var countsTask = concertModule.GetArtistDashboardCountsAsync(artistId, ct);
        var mtdPayoutsTask = now == monthStart
            ? Task.FromResult(Money.Gbp(0m))
            : paymentReportingClient.GetSettlementPayoutsAsync(
                tenantId,
                new DateRange(monthStart, now),
                ct);
        await Task.WhenAll(countsTask, mtdPayoutsTask);

        var counts = countsTask.Result;
        if (counts is null) return null;

        return new ArtistDashboardKpis(
            PendingApplications: counts.PendingApplications,
            AcceptedAwaitingCheckout: counts.AcceptedAwaitingCheckout,
            UpcomingConcerts: counts.UpcomingConcerts,
            MtdPayoutsCents: mtdPayoutsTask.Result.ToMinorUnits(),
            MtdPayoutsDeltaPercent: null);
    }

    public async Task<IReadOnlyList<MonthlyRevenuePoint>> GetPayoutsAsync(CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var firstMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);
        var points = await paymentReportingClient.GetSettlementPayoutsByMonthAsync(
            tenantId,
            new DateRange(firstMonth, now),
            ct);

        return FillMonthlySeries(points, firstMonth);
    }

    private static ProfileHealth ToProfileHealth(ArtistDetails artist, PaymentPayoutAccountStatus payoutStatus)
    {
        ProfileHealthItem[] items =
        [
            new("name", "Set artist name", "/_artist/my", !string.IsNullOrWhiteSpace(artist.Name)),
            new("bio", "Add an about section", "/_artist/my", !string.IsNullOrWhiteSpace(artist.About)),
            new("banner", "Upload a banner image", "/_artist/my", !string.IsNullOrWhiteSpace(artist.BannerUrl)),
            new("avatar", "Upload a profile image", "/_artist/my", !string.IsNullOrWhiteSpace(artist.Avatar)),
            new("genres", "Set genres", "/_artist/my", artist.Genres.Any()),
            new("stripe", "Connect Stripe payouts", "/_artist/settings/payment", payoutStatus == PaymentPayoutAccountStatus.Verified)
        ];
        var completeness = items.Count(item => item.Done) * 100 / items.Length;
        return new ProfileHealth(completeness, items);
    }

    private static StripeConnectStatus ToStripeConnectStatus(PaymentPayoutAccountStatus payoutStatus) =>
        new(
            payoutStatus switch
            {
                PaymentPayoutAccountStatus.Verified => StripeConnectState.Complete,
                PaymentPayoutAccountStatus.Pending => StripeConnectState.Pending,
                _ => StripeConnectState.Incomplete
            },
            "/_artist/settings/payment");

    private static IReadOnlyList<MonthlyRevenuePoint> FillMonthlySeries(
        IReadOnlyList<Concertable.Payment.Client.MonthlyPaymentPoint> points,
        DateTime firstMonth)
    {
        var byMonth = points.ToDictionary(point => point.Month);
        return Enumerable.Range(0, 6)
            .Select(offset => DateOnly.FromDateTime(firstMonth.AddMonths(offset)))
            .Select(month => byMonth.TryGetValue(month, out var point)
                ? new MonthlyRevenuePoint(month, point.Gross.ToMinorUnits(), point.Net.ToMinorUnits(), point.Count)
                : new MonthlyRevenuePoint(month, 0, 0, 0))
            .ToArray();
    }
}
