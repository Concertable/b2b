namespace Concertable.B2B.Privacy.Domain.Lifecycle;

/// <summary>
/// The lifecycle of a <see cref="SubjectErasureRequestEntity"/>. <c>Deferred</c> is a first-class hold, not a
/// failure: a subject with a live financial obligation waits here and the hourly sweep re-drives it to
/// <c>InProgress</c> the moment the obligation settles (Phase 5). <c>Failed</c> is reserved for a hard error
/// during the anonymisation fan-out.
/// </summary>
public enum ErasureState
{
    Requested,
    Deferred,
    InProgress,
    Completed,
    Failed,
}
