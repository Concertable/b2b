namespace Concertable.B2B.Conversations.Domain.Entities;

public sealed class ConversationCreationReceipt : IGuidEntity
{
    private ConversationCreationReceipt() { }

    public Guid Id { get; private set; }
    public int ConversationId { get; private set; }
    public Guid CreatorTenantId { get; private set; }
    public Guid CreatedByMembershipId { get; private set; }
    public Guid RequestId { get; private set; }
    public string PayloadHash { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    public static ConversationCreationReceipt Record(
        int conversationId,
        Guid creatorTenantId,
        Guid createdByMembershipId,
        Guid requestId,
        string payloadHash,
        DateTime createdAt) => new()
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            CreatorTenantId = creatorTenantId,
            CreatedByMembershipId = createdByMembershipId,
            RequestId = requestId,
            PayloadHash = payloadHash,
            CreatedAt = createdAt
        };

    public bool Matches(string payloadHash) => PayloadHash == payloadHash;
}
