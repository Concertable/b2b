using Concertable.B2B.Tenant.Domain.Events;
using Concertable.Kernel;
using Concertable.B2B.Tenant.Domain.Errors;

namespace Concertable.B2B.Tenant.Domain.Entities;

/// <summary>
/// An outstanding invitation for an email to join a tenant with a given role. The <see cref="Id"/> is the
/// accept token carried in the emailed link — there is no separate secret. Accepting creates a
/// <see cref="TenantMembershipEntity"/>; the last-Owner and already-member invariants live in the service
/// layer (an invitation can't see its peers). One live (<see cref="InvitationStatus.Pending"/>) invitation
/// per <c>(TenantId, Email)</c>; <see cref="Email"/> is stored normalized (trimmed, lower-cased) so the
/// registration-match lookup and the unique index agree.
/// </summary>
public sealed class TenantInvitationEntity : IGuidEntity, IEventRaiser
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

    private readonly EventRaiser events = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => events.DomainEvents;
    public void ClearDomainEvents() => events.Clear();

    /// <summary>Whether the invitation is still live at <paramref name="utcNow"/> — pending and unexpired. A lapsed
    /// row stays <see cref="InvitationStatus.Pending"/> in storage (nothing sweeps it), so <c>Pending</c> alone is
    /// not "live". Mirrors the Auth token entities' <c>IsActive</c>.</summary>
    public bool IsActive(DateTime utcNow) => Status == InvitationStatus.Pending && utcNow < ExpiresAt;

    public static TenantInvitationEntity Create(Guid tenantId, TenantType tenantType, string email, TenantRole role, Guid createdBy, DateTime at, TimeSpan ttl)
    {
        var invitation = new TenantInvitationEntity
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
        invitation.events.Raise(new TenantInvitationCreatedDomainEvent(invitation.Id, email, role, tenantType));
        return invitation;
    }

    /// <summary>Accepts a still-pending, unexpired invitation for <paramref name="userId"/>.</summary>
    public UnitResult<InvitationAcceptanceError> Accept(Guid userId, DateTime at)
    {
        if (Status != InvitationStatus.Pending)
            return new InvitationAcceptanceError.NotPending();
        if (at >= ExpiresAt)
            return new InvitationAcceptanceError.Expired();
        Status = InvitationStatus.Accepted;
        AcceptedByUserId = userId;
        AcceptedAt = at;
        return new Success();
    }

    /// <summary>Revokes a still-pending invitation.</summary>
    public UnitResult<InvitationRevocationError> Revoke()
    {
        if (Status != InvitationStatus.Pending)
            return new InvitationRevocationError.NotPending();
        Status = InvitationStatus.Revoked;
        return new Success();
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
