using Reunion;

namespace Concertable.B2B.Concert.Contracts;

public interface IConcertModule
{
    Task<Option<VenueDashboardCounts>> GetVenueDashboardCountsAsync(
        Guid venueTenantId,
        CancellationToken ct = default);
    Task<Option<ArtistDashboardCounts>> GetArtistDashboardCountsAsync(
        Guid artistTenantId,
        CancellationToken ct = default);
    Task<IReadOnlyList<SettlementContext>> GetSettlementContextsAsync(
        IReadOnlyCollection<int> concertIds,
        CancellationToken ct = default);

    /// <summary>GDPR erasure gate: whether any of the subject's tenants has a live financial obligation this module
    /// can see — a concert still in settlement, or a current self-billing agreement — so erasure defers rather than
    /// corrupting settlement. Fail-closed and answered tenant-less by explicit ids.</summary>
    Task<bool> HasLiveObligationsAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default);

    /// <summary>The subject's portable Concert records fragment (GDPR arts. 15/20): the RETAINED invoices and
    /// self-billing agreements their tenants are party to — read-only, never mutated by erasure.</summary>
    Task<ConcertExport> GetConcertExportAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default);
}
