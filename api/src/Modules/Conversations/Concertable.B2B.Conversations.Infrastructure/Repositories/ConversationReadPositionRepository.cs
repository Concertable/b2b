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
            DECLARE @Current bigint;
            SELECT @Current = LastReadSequence
            FROM conversations.ConversationReadPositions WITH (UPDLOCK, HOLDLOCK)
            WHERE ConversationId = {conversationId} AND MembershipId = {membershipId};

            IF @Current IS NULL
                INSERT conversations.ConversationReadPositions
                    (ConversationId, TenantId, MembershipId, LastReadSequence)
                VALUES ({conversationId}, {tenantId}, {membershipId}, {throughSequence});
            ELSE IF @Current < {throughSequence}
                UPDATE conversations.ConversationReadPositions
                SET LastReadSequence = {throughSequence}
                WHERE ConversationId = {conversationId} AND MembershipId = {membershipId};
            """, ct);
}
