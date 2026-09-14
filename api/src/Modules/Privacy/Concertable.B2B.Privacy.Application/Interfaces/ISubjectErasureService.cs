namespace Concertable.B2B.Privacy.Application.Interfaces;

/// <summary>Raises and drives a GDPR erasure for a data subject: records the request, evaluates the fail-closed
/// gate, and either runs the cross-module anonymisation fan-out to completion or defers (returning the request's
/// resulting state). Deferral and completion are both ordinary outcomes carried on the returned DTO's
/// <see cref="ErasureState"/>, so there is no expected failure to model as a Result.</summary>
internal interface ISubjectErasureService
{
    Task<SubjectErasureRequestDto> RequestErasureAsync(Guid subjectId, CancellationToken ct = default);
}
