using Concertable.B2B.Privacy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Privacy.Infrastructure.Repositories;

internal sealed class SubjectErasureRepository : Repository<SubjectErasureRequestEntity>, ISubjectErasureRepository
{
    private static readonly ErasureState[] ResumableStates =
        [ErasureState.Deferred, ErasureState.InProgress, ErasureState.Failed];

    private readonly PrivacyDbContext context;

    public SubjectErasureRepository(PrivacyDbContext context) : base(context)
    {
        this.context = context;
    }

    public Task<SubjectErasureRequestEntity?> GetBySubjectIdAsync(Guid subjectId, CancellationToken ct = default) =>
        this.context.SubjectErasureRequests.FirstOrDefaultAsync(r => r.SubjectId == subjectId, ct);

    public async Task<IReadOnlyList<Guid>> ListResumableSubjectIdsAsync(int take, CancellationToken ct = default) =>
        await this.context.SubjectErasureRequests
            .Where(r => ResumableStates.Contains(r.State))
            .OrderBy(r => r.LastAttemptedAtUtc ?? r.RequestedAtUtc)
            .ThenBy(r => r.Id)
            .Take(take)
            .Select(r => r.SubjectId)
            .ToListAsync(ct);
}
