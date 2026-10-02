using Concertable.DataAccess.Application;

namespace Concertable.B2B.Privacy.Application.Interfaces;

internal interface ISubjectErasureRepository : IRepository<SubjectErasureRequestEntity, Guid>
{
    Task<SubjectErasureRequestEntity?> GetBySubjectIdAsync(Guid subjectId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> ListResumableSubjectIdsAsync(int take, CancellationToken ct = default);
}
