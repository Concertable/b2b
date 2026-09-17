using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Contracts;

namespace Concertable.B2B.Concert.Infrastructure;

internal sealed class ConcertModule : IConcertModule
{
    private readonly IConcertDashboardService dashboardService;
    private readonly IObligationChecker obligationChecker;
    private readonly ISubjectRecordReader subjectRecordReader;

    public ConcertModule(
        IConcertDashboardService dashboardService,
        IObligationChecker obligationChecker,
        ISubjectRecordReader subjectRecordReader)
    {
        this.dashboardService = dashboardService;
        this.obligationChecker = obligationChecker;
        this.subjectRecordReader = subjectRecordReader;
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

    public Task<bool> HasLiveObligationsByTenantIdsAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default) =>
        obligationChecker.HasLiveAsync(tenantIds, ct);

    public Task<SubjectConcertRecordsDto> GetSubjectRecordsAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default) =>
        subjectRecordReader.GetSubjectRecordsAsync(tenantIds, ct);
}
