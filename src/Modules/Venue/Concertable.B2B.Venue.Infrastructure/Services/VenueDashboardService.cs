using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Venue.Application.DTOs;
using Concertable.B2B.Venue.Application.Interfaces;
using Concertable.Kernel.Identity;
using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;
using PaymentPayoutAccountStatus = Concertable.Payment.Client.Enums.PayoutAccountStatus;

namespace Concertable.B2B.Venue.Infrastructure.Services;

internal sealed class VenueDashboardService : IVenueDashboardService
{
    private readonly IVenueService venueService;
    private readonly IConcertModule concertModule;
    private readonly IVenueReviewService reviewService;
    private readonly IManagerPaymentReportingClient paymentReportingClient;
    private readonly IPayoutAccountOperationsClient payoutAccountClient;
    private readonly ITenantContext tenantContext;
    private readonly TimeProvider timeProvider;

    public VenueDashboardService(
        IVenueService venueService,
        IConcertModule concertModule,
        IVenueReviewService reviewService,
        IManagerPaymentReportingClient paymentReportingClient,
        IPayoutAccountOperationsClient payoutAccountClient,
        ITenantContext tenantContext,
        TimeProvider timeProvider)
    {
        this.venueService = venueService;
        this.concertModule = concertModule;
        this.reviewService = reviewService;
        this.paymentReportingClient = paymentReportingClient;
        this.payoutAccountClient = payoutAccountClient;
        this.tenantContext = tenantContext;
        this.timeProvider = timeProvider;
    }

    public async Task<VenueDashboardOverview?> GetOverviewAsync(CancellationToken ct = default)
    {
        var venue = await venueService.GetDetailsForCurrentUserAsync();
        if (venue is null)
            return null;

        var tenantId = tenantContext.GetTenantId();
        var reviewSummaryTask = reviewService.GetSummaryAsync(venue.Id);
        var payoutStatusTask = payoutAccountClient.GetAccountStatusAsync(tenantId, ct);
        await Task.WhenAll(reviewSummaryTask, payoutStatusTask);

        var profileHealth = ToProfileHealth(venue, payoutStatusTask.Result);
        return new VenueDashboardOverview(
            venue.Id,
            venue.Name,
            profileHealth,
            ToStripeConnectStatus(payoutStatusTask.Result),
            reviewSummaryTask.Result);
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

    public async Task<IReadOnlyList<MonthlyRevenuePoint>> GetTicketRevenueAsync(CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var firstMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);
        var points = await paymentReportingClient.GetTicketRevenueByMonthAsync(
            tenantId,
            new DateRange(firstMonth, now),
            ct);

        return FillMonthlySeries(points, firstMonth);
    }

    public async Task<IReadOnlyList<Settlement>> GetSettlementsAsync(CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var settlements = await paymentReportingClient.GetRecentSettlementsAsync(tenantId, 5, ct);
        var contexts = await concertModule.GetManagerSettlementContextsAsync(
            settlements.Select(s => s.BookingId).Distinct().ToArray(),
            ct);
        var contextsByBooking = contexts.ToDictionary(c => c.BookingId);

        return settlements
            .Where(s => contextsByBooking.ContainsKey(s.BookingId))
            .Select(s =>
            {
                var context = contextsByBooking[s.BookingId];
                var counterpartyName = context.VenueTenantId == tenantId
                    ? context.ArtistName
                    : context.VenueName;
                var direction = s.PayeeId == tenantId
                    ? SettlementDirection.In
                    : SettlementDirection.Out;

                return new Settlement(
                    s.Id,
                    context.ConcertId,
                    context.ConcertName,
                    s.At,
                    s.Amount.ToMinorUnits(),
                    counterpartyName,
                    direction);
            })
            .ToArray();
    }

    private static ProfileHealth ToProfileHealth(VenueDetails venue, PaymentPayoutAccountStatus payoutStatus)
    {
        ProfileHealthItem[] items =
        [
            new("name", "Set venue name", "/_venue/my", !string.IsNullOrWhiteSpace(venue.Name)),
            new("bio", "Add an about section", "/_venue/my", !string.IsNullOrWhiteSpace(venue.About)),
            new("banner", "Upload a banner image", "/_venue/my", !string.IsNullOrWhiteSpace(venue.BannerUrl)),
            new("avatar", "Upload a profile image", "/_venue/my", !string.IsNullOrWhiteSpace(venue.Avatar)),
            new("stripe", "Connect Stripe payouts", "/_venue/settings/payment", payoutStatus == PaymentPayoutAccountStatus.Verified)
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
            "/_venue/settings/payment");

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
