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
    private readonly IArtistService artistService;
    private readonly IConcertModule concertModule;
    private readonly IManagerPaymentReportingClient paymentReportingClient;
    private readonly ITenantContext tenantContext;
    private readonly TimeProvider timeProvider;

    public ArtistDashboardService(
        IArtistService artistService,
        IConcertModule concertModule,
        IManagerPaymentReportingClient paymentReportingClient,
        ITenantContext tenantContext,
        TimeProvider timeProvider)
    {
        this.artistService = artistService;
        this.concertModule = concertModule;
        this.paymentReportingClient = paymentReportingClient;
        this.tenantContext = tenantContext;
        this.timeProvider = timeProvider;
    }

    public async Task<Option<ArtistDashboardKpis>> GetKpisAsync(CancellationToken ct = default)
    {
        var artistIdOption = await artistService.GetIdForCurrentTenantAsync();
        if (!artistIdOption.TryGetValue(out var artistId))
            return null;
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

        return countsTask.Result.Map(counts => new ArtistDashboardKpis(
            PendingApplications: counts.PendingApplications,
            AcceptedAwaitingCheckout: counts.AcceptedAwaitingCheckout,
            UpcomingConcerts: counts.UpcomingConcerts,
            MtdPayoutsCents: mtdPayoutsTask.Result.ToMinorUnits(),
            MtdPayoutsDeltaPercent: null));
    }
}
