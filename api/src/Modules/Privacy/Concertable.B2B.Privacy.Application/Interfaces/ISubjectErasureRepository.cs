using Concertable.DataAccess.Application;

namespace Concertable.B2B.Privacy.Application.Interfaces;

internal interface ISubjectErasureRepository : IRepository<SubjectErasureRequestEntity, Guid>
{
    /// <summary>The subject's erasure request, or null when none was ever raised. One request per subject — a
    /// re-raised DSAR re-drives the existing record rather than opening a second one, so the ICO-facing history
    /// stays single-valued and a deferred request keeps its original <c>RequestedAtUtc</c> for the SLA clock.</summary>
    Task<SubjectErasureRequestEntity?> GetBySubjectIdAsync(Guid subjectId, CancellationToken ct = default);

    /// <summary>Every deferred request, oldest first — the hourly sweep's work list.</summary>
    Task<IReadOnlyList<SubjectErasureRequestEntity>> ListDeferredAsync(CancellationToken ct = default);
}
