namespace Concertable.B2B.Privacy.Application.Interfaces;

internal interface ISubjectErasureService
{
    Task<Result<SubjectErasureRequestDto, ErasureTransitionError>> RequestErasureAsync(Guid subjectId, CancellationToken ct = default);
}
