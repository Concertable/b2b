using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Contracts;

namespace Concertable.B2B.Concert.Infrastructure;

internal sealed class ConcertModule : IConcertModule
{
    private readonly IConcertDashboardService dashboardService;
    private readonly IObligationChecker obligationChecker;
    private readonly IConcertExportReader concertExportReader;

    public ConcertModule(
        IConcertDashboardService dashboardService,
        IObligationChecker obligationChecker,
        IConcertExportReader concertExportReader)
    {
        this.dashboardService = dashboardService;
        this.obligationChecker = obligationChecker;
        this.concertExportReader = concertExportReader;
    }

    public Task<Option<VenueDashboardCounts>> GetVenueDashboardCountsAsync(
        Guid venueTenantId,
        CancellationToken ct = default) =>
        dashboardService.GetVenueCountsAsync(venueTenantId, ct);

    public Task<Option<ArtistDashboardCounts>> GetArtistDashboardCountsAsync(
        Guid artistTenantId,
        CancellationToken ct = default) =>
        dashboardService.GetArtistCountsAsync(artistTenantId, ct);

    public Task<IReadOnlyList<SettlementContext>> GetSettlementContextsAsync(
        IReadOnlyCollection<int> concertIds,
        CancellationToken ct = default) =>
        dashboardService.GetSettlementContextsAsync(concertIds, ct);

    public Task<bool> HasLiveObligationsAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default) =>
        obligationChecker.HasLiveAsync(tenantIds, ct);

    public Task<ConcertExport> GetConcertExportAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default) =>
        concertExportReader.GetConcertExportAsync(tenantIds, ct);
}
