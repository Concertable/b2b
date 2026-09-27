using Concertable.B2B.Conversations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Repositories;

internal sealed class ConversationReadPositionRepository(
    ConversationsPrivilegedDbContext context) : IConversationReadPositionRepository
{
    public async Task AdvanceAsync(
        int conversationId,
        Guid tenantId,
        Guid membershipId,
        long throughSequence,
        CancellationToken ct = default) =>
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO conversations."ConversationReadPositions"
                ("ConversationId", "TenantId", "MembershipId", "LastReadSequence")
            VALUES ({conversationId}, {tenantId}, {membershipId}, {throughSequence})
            ON CONFLICT ("ConversationId", "MembershipId")
            DO UPDATE SET "LastReadSequence" = GREATEST(
                conversations."ConversationReadPositions"."LastReadSequence",
                EXCLUDED."LastReadSequence");
            """, ct);
}
