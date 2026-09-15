namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>Counts the live financial obligations a subject's tenants carry in this module — a concert still in
/// settlement, or a current self-billing agreement — answered across all tenants over the unfiltered read stance
/// so it works tenant-less from the admin erasure flow. A precondition read the GDPR erasure flow turns into a
/// deferral; it does not throw.</summary>
internal interface IObligationChecker
{
    Task<int> CountLiveAsync(IReadOnlyCollection<Guid> tenantIds, CancellationToken ct = default);
}
