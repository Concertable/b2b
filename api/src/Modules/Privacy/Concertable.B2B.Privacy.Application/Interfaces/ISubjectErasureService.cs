namespace Concertable.B2B.Privacy.Application.Interfaces;

/// <summary>Owns the subject-erasure aggregate and drives it. One request per subject: raising a DSAR for a
/// subject who already has one re-drives that record rather than opening a second, so a deferred request keeps
/// the <c>RequestedAtUtc</c> the statutory one-calendar-month clock runs from.</summary>
internal interface ISubjectErasureService
{
    /// <summary>Raises — or re-drives — the subject's erasure. Completed is terminal and returned untouched.</summary>
    Task<Result<SubjectErasureRequestDto, ErasureTransitionError>> RequestErasureAsync(Guid subjectId, CancellationToken ct = default);
}
