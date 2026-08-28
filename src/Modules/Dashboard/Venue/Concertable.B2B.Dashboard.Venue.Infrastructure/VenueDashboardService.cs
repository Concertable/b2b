using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Dashboard.Contracts;
using Concertable.B2B.Dashboard.Venue.Application;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Venue.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;
using Reunion;

namespace Concertable.B2B.Dashboard.Venue.Infrastructure;

internal sealed class VenueDashboardService : IVenueDashboardService
{
    private readonly IApplicationModule applicationModule;
    private readonly IConcertModule concertModule;
    private readonly IOpportunityModule opportunityModule;
    private readonly IManagerPaymentReportingClient paymentReportingClient;
    private readonly IPayoutAccountOperationsClient payoutAccountClient;
    private readonly ITenantContext tenantContext;
    private readonly ITenantModule tenantModule;
    private readonly TimeProvider timeProvider;
    private readonly IVenueModule venueModule;

    public VenueDashboardService(
        IApplicationModule applicationModule,
        IConcertModule concertModule,
        IOpportunityModule opportunityModule,
        IManagerPaymentReportingClient paymentReportingClient,
        IPayoutAccountOperationsClient payoutAccountClient,
        ITenantContext tenantContext,
        ITenantModule tenantModule,
        TimeProvider timeProvider,
        IVenueModule venueModule)
    {
        this.applicationModule = applicationModule;
        this.concertModule = concertModule;
        this.opportunityModule = opportunityModule;
        this.paymentReportingClient = paymentReportingClient;
        this.payoutAccountClient = payoutAccountClient;
        this.tenantContext = tenantContext;
        this.tenantModule = tenantModule;
        this.timeProvider = timeProvider;
        this.venueModule = venueModule;
    }

    public async Task<Option<VenueDashboardKpis>> GetAsync(CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var period = DashboardReportingPeriod.From(timeProvider.GetUtcNow().UtcDateTime);
        var pendingApplicationsTask = applicationModule.GetVenuePendingCountAsync(tenantId, ct);
        var openOpportunitiesTask = opportunityModule.GetOpenCountAsync(tenantId, ct);
        var concertCountsTask = concertModule.GetVenueDashboardCountsAsync(tenantId, ct);
        var revenueTask = period.HasElapsedTime
            ? paymentReportingClient.GetTicketRevenueAsync(
                tenantId,
                new DateRange(period.MonthStart, period.Now),
                ct)
            : Task.FromResult(Money.Gbp(0m));

        await Task.WhenAll(pendingApplicationsTask, openOpportunitiesTask, concertCountsTask, revenueTask);
        var concertCounts = await concertCountsTask;
        if (!concertCounts.TryGetValue(out var counts))
            return null;

        return new VenueDashboardKpis(
            await pendingApplicationsTask,
            null,
            await openOpportunitiesTask,
            counts.UpcomingConcerts,
            counts.AwaitingDoorRevenue,
            (await revenueTask).ToMinorUnits(),
            null);
    }

    public async Task<Option<VenueDashboardOverview>> GetOverviewAsync(
        CancellationToken ct = default) =>
        await (await venueModule.GetCurrentProfileAsync(ct))
            .MapAsync(async venue =>
            {
                var tenantId = tenantContext.GetTenantId();
                var reviewSummaryTask = venueModule.GetReviewSummaryAsync(venue.Id, ct);
                var payoutStatusTask = payoutAccountClient.GetAccountStatusAsync(tenantId, ct);
                await Task.WhenAll(reviewSummaryTask, payoutStatusTask);

                var payoutStatus = await payoutStatusTask;
                return new VenueDashboardOverview(
                    venue.Id,
                    venue.Name,
                    venue.ToProfileHealth(payoutStatus),
                    payoutStatus.ToStripeConnectStatus(),
                    await reviewSummaryTask);
            });

    public async Task<IReadOnlyList<MonthlyRevenuePoint>> GetTicketRevenueAsync(
        CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var firstMonth = DashboardReportingPeriod.From(now).MonthStart.AddMonths(-5);
        var points = await paymentReportingClient.GetTicketRevenueByMonthAsync(
            tenantId,
            new DateRange(firstMonth, now),
            ct);

        return points.ToMonthlyRevenuePoints(firstMonth);
    }

    public async Task<IReadOnlyList<Settlement>> GetSettlementsAsync(
        CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var settlements = await paymentReportingClient.GetRecentSettlementsAsync(tenantId, 5, ct);
        var contexts = await concertModule.GetManagerSettlementContextsAsync(
            settlements.Select(settlement => settlement.BookingId).Distinct().ToArray(),
            ct);
        var contextsByBooking = contexts.ToDictionary(context => context.BookingId);

        return settlements
            .Where(settlement => contextsByBooking.ContainsKey(settlement.BookingId))
            .Select(settlement =>
            {
                var context = contextsByBooking[settlement.BookingId];
                var counterpartyName = context.VenueTenantId == tenantId
                    ? context.ArtistName
                    : context.VenueName;
                var direction = settlement.PayeeId == tenantId
                    ? SettlementDirection.In
                    : SettlementDirection.Out;

                return new Settlement(
                    settlement.Id,
                    context.ConcertId,
                    context.ConcertName,
                    settlement.At,
                    settlement.Amount.ToMinorUnits(),
                    counterpartyName,
                    direction);
            })
            .ToArray();
    }

    public Task<IReadOnlyList<ActivityItemDto>> GetActivityAsync(
        CancellationToken ct = default) =>
        tenantModule.GetRecentActivityAsync(tenantContext.GetTenantId(), 10, ct);
}
