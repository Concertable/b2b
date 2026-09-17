namespace Concertable.B2B.Booking.Application.Interfaces;

/// <summary>Whether any of the subject's tenants holds a booking that is still committing money — anything short
/// of a cancelled booking. Answered across all tenants over the unfiltered read stance, so it works tenant-less
/// from the admin erasure flow. A precondition the GDPR erasure flow turns into a deferral; it does not throw.</summary>
internal interface IObligationChecker
{
    Task<bool> HasLiveAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default);
}
