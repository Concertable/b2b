using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain.Entities;

/// <summary>
/// An outstanding invitation for an email to join a tenant with a given role. The <see cref="Id"/> is the
/// accept token carried in the emailed link — there is no separate secret. Accepting creates a
/// <see cref="TenantMembershipEntity"/>; the last-Owner and already-member invariants live in the service
/// layer (an invitation can't see its peers). One live (<see cref="InvitationStatus.Pending"/>) invitation
/// per <c>(TenantId, Email)</c>; <see cref="Email"/> is stored normalized (trimmed, lower-cased) so the
/// registration-match lookup and the unique index agree.
/// </summary>
public sealed class TenantInvitationEntity : IGuidEntity
{
    private TenantInvitationEntity() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    /// <summary>Normalized (trimmed, lower-cased) invitee email — the registration-match key.</summary>
    public string Email { get; private set; } = null!;
    public TenantRole Role { get; private set; }
    public InvitationStatus Status { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public Guid? AcceptedByUserId { get; private set; }
    public DateTime? AcceptedAt { get; private set; }

    /// <summary>Whether the invitation is still live at <paramref name="utcNow"/> — pending and unexpired. A lapsed
    /// row stays <see cref="InvitationStatus.Pending"/> in storage (nothing sweeps it), so <c>Pending</c> alone is
    /// not "live". Mirrors the Auth token entities' <c>IsActive</c>.</summary>
    public bool IsActive(DateTime utcNow) => Status == InvitationStatus.Pending && utcNow < ExpiresAt;

    public static TenantInvitationEntity Create(Guid tenantId, string email, TenantRole role, Guid createdBy, DateTime at, TimeSpan ttl) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            Role = role,
            Status = InvitationStatus.Pending,
            CreatedByUserId = createdBy,
            CreatedAt = at,
            ExpiresAt = at + ttl,
        };

    /// <summary>Accepts a still-pending, unexpired invitation for <paramref name="userId"/>.</summary>
    public void Accept(Guid userId, DateTime at)
    {
        if (Status != InvitationStatus.Pending)
            throw new DomainException("Invitation is not pending.");
        if (at >= ExpiresAt)
            throw new DomainException("Invitation has expired.");
        Status = InvitationStatus.Accepted;
        AcceptedByUserId = userId;
        AcceptedAt = at;
    }

    /// <summary>Revokes a still-pending invitation.</summary>
    public void Revoke()
    {
        if (Status != InvitationStatus.Pending)
            throw new DomainException("Only a pending invitation can be revoked.");
        Status = InvitationStatus.Revoked;
    }

    /// <summary>Retires a lapsed invitation. The row stays <see cref="InvitationStatus.Pending"/> once its TTL
    /// passes (nothing sweeps it), so a re-invite calls this to free the <c>(TenantId, Email)</c> filtered-unique
    /// slot the stale row still occupies.</summary>
    public void Expire()
    {
        if (Status != InvitationStatus.Pending)
            throw new DomainException("Only a pending invitation can expire.");
        Status = InvitationStatus.Expired;
    }
}
