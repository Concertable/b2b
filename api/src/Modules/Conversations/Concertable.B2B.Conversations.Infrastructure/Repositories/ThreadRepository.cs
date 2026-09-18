using Concertable.B2B.Conversations.Contracts.Enums;
using Concertable.B2B.Conversations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Repositories;

internal sealed class ThreadRepository : Repository<ThreadEntity>, IThreadRepository
{
    private readonly ConversationsDbContext context;

    public ThreadRepository(ConversationsDbContext context) : base(context)
    {
        this.context = context;
    }

    public async Task<ThreadEntity?> GetByParticipantsAsync(
        IReadOnlyCollection<Guid> participantTenantIds,
        CancellationToken ct = default)
    {
        // Exactly these participants, not merely these among others: a wider conversation is a different one.
        var threadId = await context.ThreadAccessGrants
            .Where(grant => grant.Scope == ThreadAccessScope.Participate && grant.RevokedAt == null)
            .GroupBy(grant => grant.ResourceId)
            .Where(grants =>
                grants.Select(grant => grant.TenantId).Distinct().Count() == participantTenantIds.Count
                && grants.All(grant => participantTenantIds.Contains(grant.TenantId)))
            .Select(grants => (int?)grants.Key)
            .FirstOrDefaultAsync(ct);

        return threadId is { } id
            ? await context.Threads.FirstOrDefaultAsync(thread => thread.Id == id, ct)
            : null;
    }

    public async Task<IReadOnlyList<Guid>> GetParticipantTenantIdsAsync(int threadId, CancellationToken ct = default) =>
        await context.ThreadAccessGrants
            .Where(grant =>
                grant.ResourceId == threadId
                && grant.Scope == ThreadAccessScope.Participate
                && grant.RevokedAt == null)
            .Select(grant => grant.TenantId)
            .Distinct()
            .ToListAsync(ct);
}
