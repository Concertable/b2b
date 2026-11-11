using Concertable.B2B.Conversations.Domain.Entities;
using Concertable.B2B.Conversations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Repositories;

internal sealed class ConversationReadPositionRepository : IConversationReadPositionRepository
{
    private readonly ConversationsPrivilegedDbContext context;

    public ConversationReadPositionRepository(ConversationsPrivilegedDbContext context)
    {
        this.context = context;
    }

    public Task<ConversationReadPosition?> GetByConversationIdAndMembershipIdAsync(
        int conversationId,
        Guid membershipId,
        CancellationToken ct = default) =>
        context.ConversationReadPositions.SingleOrDefaultAsync(
            position => position.ConversationId == conversationId && position.MembershipId == membershipId,
            ct);

    public void Add(ConversationReadPosition position) =>
        context.ConversationReadPositions.Add(position);

    public Task SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
