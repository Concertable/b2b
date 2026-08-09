using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Conversations.Domain.Entities;

/// <summary>A member's read watermark over one tenant-pair thread: unread = thread messages with
/// <c>SentDate &gt; LastReadAt</c> the member didn't send. One row per (thread pair, member) — not per
/// message — so read state is O(members), not O(members × messages).</summary>
public sealed class ThreadReadStateEntity : IIdEntity, IVenueArtistTenantScoped
{
    private ThreadReadStateEntity() { }

    public int Id { get; private set; }
    public Guid VenueTenantId { get; private set; }
    public Guid ArtistTenantId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime LastReadAt { get; private set; }

    public static ThreadReadStateEntity Create(Guid venueTenantId, Guid artistTenantId, Guid userId, DateTime lastReadAt) => new()
    {
        VenueTenantId = venueTenantId,
        ArtistTenantId = artistTenantId,
        UserId = userId,
        LastReadAt = lastReadAt
    };

    public void Advance(DateTime readAt)
    {
        if (readAt > LastReadAt)
            LastReadAt = readAt;
    }
}
