using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Venue.Application.DTOs;
using Concertable.B2B.Venue.Application.Interfaces;
using Concertable.B2B.Venue.Infrastructure.Extensions;
using Concertable.B2B.Venue.Infrastructure.Mappers;
using Concertable.Kernel.Identity;
using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;

namespace Concertable.B2B.Venue.Infrastructure.Services;

internal sealed class VenueDashboardService : IVenueDashboardService
{
    private readonly IVenueService venueService;
    private readonly IConcertModule concertModule;
    private readonly IVenueReviewService reviewService;
    private readonly IManagerPaymentReportingClient paymentReportingClient;
    private readonly IPayoutAccountOperationsClient payoutAccountClient;
    private readonly ITenantContext tenantContext;
    private readonly ITenantModule tenantModule;
    private readonly TimeProvider timeProvider;

    public VenueDashboardService(
        IVenueService venueService,
        IConcertModule concertModule,
        IVenueReviewService reviewService,
        IManagerPaymentReportingClient paymentReportingClient,
        IPayoutAccountOperationsClient payoutAccountClient,
        ITenantContext tenantContext,
        ITenantModule tenantModule,
        TimeProvider timeProvider)
    {
        this.venueService = venueService;
        this.concertModule = concertModule;
        this.reviewService = reviewService;
        this.paymentReportingClient = paymentReportingClient;
        this.payoutAccountClient = payoutAccountClient;
        this.tenantContext = tenantContext;
        this.tenantModule = tenantModule;
        this.timeProvider = timeProvider;
    }

    public async Task<Option<VenueDashboardOverview>> GetOverviewAsync(CancellationToken ct = default) =>
        await (await venueService.GetDetailsAsync(ct))
            .MapAsync(async venue =>
            {
                var tenantId = tenantContext.GetTenantId();
                var reviewSummaryTask = reviewService.GetSummaryAsync(venue.Id, ct);
                var payoutStatusTask = payoutAccountClient.GetAccountStatusAsync(tenantId, ct);
                await Task.WhenAll(reviewSummaryTask, payoutStatusTask);

                var reviewSummary = await reviewSummaryTask;
                var payoutStatus = await payoutStatusTask;
                return new VenueDashboardOverview(
                    venue.Id,
                    venue.Name,
                    venue.ToProfileHealth(payoutStatus),
                    payoutStatus.ToStripeConnectStatus(),
                    reviewSummary);
            });

    public async Task<Option<VenueDashboardKpis>> GetKpisAsync(CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var monthStart = now.StartOfMonth();
        var countsTask = concertModule.GetVenueDashboardCountsAsync(tenantId, ct);
        var mtdRevenueTask = now == monthStart
            ? Task.FromResult(Money.Gbp(0m))
            : paymentReportingClient.GetTicketRevenueAsync(
                tenantId,
                new DateRange(monthStart, now),
                ct);
        await Task.WhenAll(countsTask, mtdRevenueTask);

        var countsOption = await countsTask;
        if (!countsOption.TryGetValue(out var counts))
            return null;
        var mtdRevenue = await mtdRevenueTask;

        return new VenueDashboardKpis(
            ApplicationsToReview: counts.ApplicationsToReview,
            ApplicationsToReviewDelta: null,
            OpenOpportunities: counts.OpenOpportunities,
            UpcomingConcerts: counts.UpcomingConcerts,
            AwaitingDoorRevenue: counts.AwaitingDoorRevenue,
            MtdRevenueCents: mtdRevenue.ToMinorUnits(),
            MtdRevenueDeltaPercent: null);
    }

    public async Task<IReadOnlyList<MonthlyRevenuePoint>> GetTicketRevenueAsync(CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var firstMonth = now.StartOfMonth().AddMonths(-5);
        var points = await paymentReportingClient.GetTicketRevenueByMonthAsync(
            tenantId,
            new DateRange(firstMonth, now),
            ct);

        return points.ToMonthlyRevenuePoints(firstMonth);
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

    public Task<IReadOnlyList<ActivityItemDto>> GetActivityAsync(CancellationToken ct = default) =>
        tenantModule.GetRecentActivityAsync(tenantContext.GetTenantId(), 10, ct);
}
