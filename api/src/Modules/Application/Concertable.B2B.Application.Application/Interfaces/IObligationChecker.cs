namespace Concertable.B2B.Application.Application.Interfaces;

/// <summary>Whether any of the subject's tenants holds an application that has committed money but not yet
/// reached a booking. Answered across all tenants over the unfiltered read stance, so it works tenant-less from
/// the admin erasure flow. A precondition the GDPR erasure flow turns into a deferral; it does not throw.</summary>
internal interface IObligationChecker
{
    Task<bool> HasLiveAsync(IReadOnlyCollection<Guid> tenantIds, CancellationToken ct = default);
}
