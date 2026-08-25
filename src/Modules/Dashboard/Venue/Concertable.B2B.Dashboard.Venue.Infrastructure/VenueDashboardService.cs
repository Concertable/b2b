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
    private readonly IApplicationModule applications;
    private readonly IConcertModule concerts;
    private readonly IOpportunityModule opportunities;
    private readonly IManagerPaymentReportingClient paymentReportingClient;
    private readonly IPayoutAccountOperationsClient payoutAccountClient;
    private readonly ITenantContext tenantContext;
    private readonly ITenantModule tenants;
    private readonly TimeProvider timeProvider;
    private readonly IVenueModule venues;

    public VenueDashboardService(
        IApplicationModule applications,
        IConcertModule concerts,
        IOpportunityModule opportunities,
        IManagerPaymentReportingClient paymentReportingClient,
        IPayoutAccountOperationsClient payoutAccountClient,
        ITenantContext tenantContext,
        ITenantModule tenants,
        TimeProvider timeProvider,
        IVenueModule venues)
    {
        this.applications = applications;
        this.concerts = concerts;
        this.opportunities = opportunities;
        this.paymentReportingClient = paymentReportingClient;
        this.payoutAccountClient = payoutAccountClient;
        this.tenantContext = tenantContext;
        this.tenants = tenants;
        this.timeProvider = timeProvider;
        this.venues = venues;
    }

    public async Task<Option<VenueDashboardKpis>> GetAsync(CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var period = DashboardReportingPeriod.From(timeProvider.GetUtcNow().UtcDateTime);
        var pendingApplications = await applications.GetVenuePendingCountAsync(tenantId, ct);
        var openOpportunitiesTask = opportunities.GetOpenCountAsync(tenantId, ct);
        var concertCountsTask = concerts.GetVenueDashboardCountsAsync(tenantId, ct);
        var revenueTask = period.HasElapsedTime
            ? paymentReportingClient.GetTicketRevenueAsync(
                tenantId,
                new DateRange(period.MonthStart, period.Now),
                ct)
            : Task.FromResult(Money.Gbp(0m));

        await Task.WhenAll(openOpportunitiesTask, concertCountsTask, revenueTask);
        var concertCounts = await concertCountsTask;
        if (!concertCounts.TryGetValue(out var counts))
            return null;

        return new VenueDashboardKpis(
            pendingApplications,
            null,
            await openOpportunitiesTask,
            counts.UpcomingConcerts,
            counts.AwaitingDoorRevenue,
            (await revenueTask).ToMinorUnits(),
            null);
    }

    public async Task<Option<VenueDashboardOverview>> GetOverviewAsync(
        CancellationToken ct = default) =>
        await (await venues.GetCurrentProfileAsync(ct))
            .MapAsync(async venue =>
            {
                var tenantId = tenantContext.GetTenantId();
                var reviewSummaryTask = venues.GetReviewSummaryAsync(venue.Id, ct);
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
        var contexts = await concerts.GetManagerSettlementContextsAsync(
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
        tenants.GetRecentActivityAsync(tenantContext.GetTenantId(), 10, ct);
}
