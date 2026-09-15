namespace Concertable.B2B.Application.Application.Interfaces;

/// <summary>Counts the applications a subject's tenants carry that have committed money but not yet reached a
/// booking — answered across all tenants over the unfiltered read stance so it works tenant-less from the admin
/// erasure flow. A precondition read the GDPR erasure flow turns into a deferral; it does not throw.</summary>
internal interface IObligationChecker
{
    Task<int> CountLiveAsync(IReadOnlyCollection<Guid> tenantIds, CancellationToken ct = default);
}
