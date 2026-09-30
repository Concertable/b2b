using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Conversations.Domain.Entities;

public sealed class ConversationCreationReceipt : IGuidEntity
{
    private ConversationCreationReceipt() { }

    public Guid Id { get; private set; }
    public int ConversationId { get; private set; }
    public Guid CreatorTenantId { get; private set; }
    public Guid CreatedByMembershipId { get; private set; }
    public Guid RequestId { get; private set; }
    public CommandPayloadHash PayloadHash { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    public static ConversationCreationReceipt Record(
        int conversationId,
        Guid creatorTenantId,
        Guid createdByMembershipId,
        Guid requestId,
        CommandPayloadHash payloadHash,
        DateTime createdAt) => new()
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            CreatorTenantId = creatorTenantId,
            CreatedByMembershipId = createdByMembershipId,
            RequestId = requestId,
            PayloadHash = payloadHash ?? throw new ArgumentNullException(nameof(payloadHash)),
            CreatedAt = createdAt
        };

    public bool Matches(CommandPayloadHash payloadHash) => PayloadHash ==
        (payloadHash ?? throw new ArgumentNullException(nameof(payloadHash)));
}
