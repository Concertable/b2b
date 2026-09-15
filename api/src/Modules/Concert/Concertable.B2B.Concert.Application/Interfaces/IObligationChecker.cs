namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>Whether any of the subject's tenants holds a concert still in settlement, or a current self-billing
/// agreement. Answered across all tenants over the unfiltered read stance, so it works tenant-less from the admin
/// erasure flow. A precondition the GDPR erasure flow turns into a deferral; it does not throw.</summary>
internal interface IObligationChecker
{
    Task<bool> HasLiveAsync(IReadOnlyCollection<Guid> tenantIds, CancellationToken ct = default);
}
