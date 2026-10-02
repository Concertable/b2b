namespace Concertable.B2B.Privacy.Application.Interfaces;

internal interface ISubjectObligationChecker
{
    Task<bool> HasLiveObligationsAsync(Guid subjectId, IReadOnlySet<Guid> capturedTenantIds, CancellationToken ct = default);
}
