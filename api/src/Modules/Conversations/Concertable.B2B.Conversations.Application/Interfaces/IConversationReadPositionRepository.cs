namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IConversationReadPositionRepository
{
    Task AdvanceAsync(
        int conversationId,
        Guid tenantId,
        Guid membershipId,
        long throughSequence,
        CancellationToken ct = default);
}
