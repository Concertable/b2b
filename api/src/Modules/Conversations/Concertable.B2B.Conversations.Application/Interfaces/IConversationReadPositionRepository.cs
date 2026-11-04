using Concertable.B2B.Conversations.Domain.Entities;

namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IConversationReadPositionRepository
{
    Task<ConversationReadPosition?> GetByConversationIdAndMembershipIdAsync(
        int conversationId,
        Guid membershipId,
        CancellationToken ct = default);

    void Add(ConversationReadPosition position);

    Task SaveChangesAsync(CancellationToken ct = default);
}
