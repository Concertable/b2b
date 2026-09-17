namespace Concertable.B2B.Privacy.Application.Interfaces;

/// <summary>Checks whether a subject has any live financial obligation that must defer their erasure — by
/// fanning the subject's tenants out to the modules that can see an obligation (Concert today; Payment joins
/// in Phase 4). A boolean precondition read: <c>true</c> defers. It never throws to signal it.</summary>
internal interface ISubjectObligationChecker
{
    Task<bool> HasLiveObligationsAsync(Guid subjectId, CancellationToken ct = default);
}
