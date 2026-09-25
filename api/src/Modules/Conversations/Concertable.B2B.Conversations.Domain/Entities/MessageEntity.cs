namespace Concertable.B2B.Conversations.Domain.Entities;

public sealed class MessageEntity : IIdEntity
{
    private MessageEntity() { }

    public int Id { get; private set; }
    public int ConversationId { get; private set; }
    public long Sequence { get; private set; }
    public Guid RequestId { get; private set; }
    public string PayloadHash { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public Guid SenderTenantId { get; private set; }
    public Guid SentByMembershipId { get; private set; }
    public Guid SentByUserId { get; private set; }
    public MessageAction? Action { get; private set; }
    public DateTime SentAt { get; private set; }
    public DateTime? HiddenAt { get; private set; }
    public Guid? HiddenByUserId { get; private set; }
    public DateTime? RestoredAt { get; private set; }
    public Guid? RestoredByUserId { get; private set; }

    public bool IsHidden => HiddenAt is not null && (RestoredAt is null || RestoredAt < HiddenAt);

    public static MessageEntity Create(
        int conversationId,
        long sequence,
        Guid requestId,
        string payloadHash,
        Guid senderTenantId,
        Guid sentByMembershipId,
        Guid sentByUserId,
        string content,
        DateTime sentAt,
        MessageAction? action = null) => new()
        {
            ConversationId = conversationId,
            Sequence = sequence,
            RequestId = requestId,
            PayloadHash = payloadHash,
            SenderTenantId = senderTenantId,
            SentByMembershipId = sentByMembershipId,
            SentByUserId = sentByUserId,
            Content = content,
            SentAt = sentAt,
            Action = action
        };

    public void Hide(Guid byUserId, DateTime at)
    {
        HiddenAt = at;
        HiddenByUserId = byUserId;
    }

    public void Restore(Guid byUserId, DateTime at)
    {
        RestoredAt = at;
        RestoredByUserId = byUserId;
    }
}
