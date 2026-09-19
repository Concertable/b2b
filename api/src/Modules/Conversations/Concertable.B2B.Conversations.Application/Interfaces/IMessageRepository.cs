using Concertable.B2B.Conversations.Application.DTOs;

namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IMessageRepository
{
    Task<MessageEntity?> GetByIdAsync(int messageId, CancellationToken ct = default);
    Task<IReadOnlyList<MessageEntity>> GetByConversationIdAsync(
        int conversationId,
        CancellationToken ct = default);
    Task<bool> ContainsSequenceAsync(
        int conversationId,
        long sequence,
        CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(
        Guid tenantId,
        Guid membershipId,
        CancellationToken ct = default);
    Task<IReadOnlyList<MessagePreview>> GetRecentPreviewsAsync(
        Guid tenantId,
        Guid membershipId,
        CancellationToken ct = default);
}
