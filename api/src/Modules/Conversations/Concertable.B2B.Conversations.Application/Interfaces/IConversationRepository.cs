using Concertable.DataAccess.Application;

namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IConversationRepository : IRepository<ConversationEntity>
;

internal interface IConversationPrivilegedRepository
{
    Task<ConversationEntity?> GetWithGrantsByIdAsync(int conversationId, CancellationToken ct = default);
    Task<ConversationEntity?> GetWithGrantsByIdForUpdateAsync(int conversationId, CancellationToken ct = default);
    Task<ConversationCreationReceipt?> GetCreationReceiptForUpdateAsync(
        Guid creatorTenantId,
        Guid createdByMembershipId,
        Guid requestId,
        CancellationToken ct = default);
    Task<MessageEntity?> GetMessageReceiptForUpdateAsync(
        int conversationId,
        Guid sentByMembershipId,
        Guid requestId,
        CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, TenantDisplay>> GetTenantDisplaysAsync(
        IReadOnlySet<Guid> tenantIds,
        CancellationToken ct = default);
    void Add(ConversationEntity conversation);
    void Add(ConversationCreationReceipt receipt);
    void Add(MessageEntity message);
    Task SaveChangesAsync(CancellationToken ct = default);
}
