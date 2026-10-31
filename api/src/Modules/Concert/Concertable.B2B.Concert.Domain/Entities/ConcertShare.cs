namespace Concertable.B2B.Concert.Domain.Entities;

public sealed class ConcertShare
{
    private ConcertShare() { }

    public Guid Id { get; private set; }
    public Guid RecipientTenantId { get; private set; }
    public Guid? RecipientMembershipId { get; private set; }
    public Guid SharedByTenantId { get; private set; }
    public Guid SharedByUserId { get; private set; }
    public DateTime SharedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    public bool IsLiveAt(DateTime at) => ExpiresAt is null || ExpiresAt > at;

    internal static ConcertShare Create(
        Guid recipientTenantId,
        Guid? recipientMembershipId,
        Guid sharedByTenantId,
        Guid sharedByUserId,
        DateTime sharedAt,
        DateTime? expiresAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            RecipientTenantId = recipientTenantId,
            RecipientMembershipId = recipientMembershipId,
            SharedByTenantId = sharedByTenantId,
            SharedByUserId = sharedByUserId,
            SharedAt = sharedAt,
            ExpiresAt = expiresAt,
        };
}
