namespace Concertable.B2B.Conversations.Domain.Entities;

public sealed class ConversationReadPosition : IIdEntity
{
    private ConversationReadPosition() { }

    public int Id { get; private set; }
    public int ConversationId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid MembershipId { get; private set; }
    public long LastReadSequence { get; private set; }

    public static ConversationReadPosition Create(
        int conversationId,
        Guid tenantId,
        Guid membershipId,
        long lastReadSequence) => new()
    {
        ConversationId = conversationId,
        TenantId = tenantId,
        MembershipId = membershipId,
        LastReadSequence = lastReadSequence
    };

    public void Advance(long throughSequence)
    {
        if (throughSequence > LastReadSequence)
            LastReadSequence = throughSequence;
    }
}
