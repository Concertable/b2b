using Concertable.B2B.Privacy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Privacy.Infrastructure.Repositories;

internal sealed class SubjectErasureRepository(PrivacyDbContext context)
    : Repository<SubjectErasureRequestEntity>(context), ISubjectErasureRepository
{
    private static readonly ErasureState[] ResumableStates =
        [ErasureState.Deferred, ErasureState.InProgress, ErasureState.Failed];

    public Task<SubjectErasureRequestEntity?> GetBySubjectIdAsync(Guid subjectId, CancellationToken ct = default) =>
        context.SubjectErasureRequests.FirstOrDefaultAsync(r => r.SubjectId == subjectId, ct);

    public async Task<IReadOnlyList<Guid>> ListResumableSubjectIdsAsync(int take, CancellationToken ct = default) =>
        await context.SubjectErasureRequests
            .Where(r => ResumableStates.Contains(r.State))
            .OrderBy(r => r.RequestedAtUtc)
            .Take(take)
            .Select(r => r.SubjectId)
            .ToListAsync(ct);
}
