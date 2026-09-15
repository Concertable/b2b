using Concertable.B2B.Privacy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Privacy.Infrastructure.Repositories;

internal sealed class SubjectErasureRepository(PrivacyDbContext context)
    : Repository<SubjectErasureRequestEntity>(context), ISubjectErasureRepository
{
    public Task<SubjectErasureRequestEntity?> GetBySubjectIdAsync(Guid subjectId, CancellationToken ct = default) =>
        context.SubjectErasureRequests.FirstOrDefaultAsync(r => r.SubjectId == subjectId, ct);

    public async Task<IReadOnlyList<SubjectErasureRequestEntity>> ListDeferredAsync(CancellationToken ct = default) =>
        await context.SubjectErasureRequests
            .Where(r => r.State == ErasureState.Deferred)
            .OrderBy(r => r.RequestedAtUtc)
            .ToListAsync(ct);
}
