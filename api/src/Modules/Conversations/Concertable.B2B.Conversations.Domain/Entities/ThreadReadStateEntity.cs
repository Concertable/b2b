namespace Concertable.B2B.Conversations.Domain.Entities;

/// <summary>A member's read watermark over one thread: unread = thread messages with
/// <c>SentDate &gt; LastReadAt</c> the member's own tenant didn't send. One row per (thread, tenant, member)
/// — not per message — so read state is O(members), not O(members × messages). The tenant is on the row
/// because a member can belong to several businesses and reads the thread separately in each.</summary>
public sealed class ThreadReadStateEntity : IIdEntity
{
    private ThreadReadStateEntity() { }

    public int Id { get; private set; }
    public int ThreadId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime LastReadAt { get; private set; }

    public static ThreadReadStateEntity Create(int threadId, Guid tenantId, Guid userId, DateTime lastReadAt) => new()
    {
        ThreadId = threadId,
        TenantId = tenantId,
        UserId = userId,
        LastReadAt = lastReadAt
    };

    public void Advance(DateTime readAt)
    {
        if (readAt > LastReadAt)
            LastReadAt = readAt;
    }
}
