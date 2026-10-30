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
    public IdempotencyHash IdempotencyHash { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static ConversationCreationReceipt Record(
        int conversationId,
        Guid creatorTenantId,
        Guid createdByMembershipId,
        Guid requestId,
        IdempotencyHash idempotencyHash,
        DateTime createdAt) => new()
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            CreatorTenantId = creatorTenantId,
            CreatedByMembershipId = createdByMembershipId,
            RequestId = requestId,
            IdempotencyHash = idempotencyHash != default
                ? idempotencyHash
                : throw new ArgumentException("An idempotency hash is required.", nameof(idempotencyHash)),
            CreatedAt = createdAt
        };

    public bool Matches(IdempotencyHash idempotencyHash) => IdempotencyHash ==
        (idempotencyHash != default
            ? idempotencyHash
            : throw new ArgumentException("An idempotency hash is required.", nameof(idempotencyHash)));
}
