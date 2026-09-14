using Concertable.B2B.Conversations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Repositories;

internal sealed class ParticipantProfilePrivilegedRepository : IParticipantProfilePrivilegedRepository
{
    private readonly ConversationsPrivilegedDbContext context;

    public ParticipantProfilePrivilegedRepository(ConversationsPrivilegedDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<ParticipantProfile>> ListByTenantIdsAsync(IReadOnlyList<Guid> tenantIds, CancellationToken ct = default) =>
        await context.ParticipantProfiles.Where(p => tenantIds.Contains(p.TenantId)).ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
