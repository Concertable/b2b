using Concertable.B2B.Admin.Domain.Errors;
using Concertable.B2B.Admin.Domain.Events;
using Concertable.Kernel;

namespace Concertable.B2B.Admin.Domain.Entities;

/// <summary>
/// An outstanding invitation for an email to become a Concertable admin. Unlike
/// <c>TenantInvitationEntity</c> there is no accept endpoint — acceptance is implicit when
/// <c>AdminService.GrantIfEligibleAsync</c> sees a matching, active invitation at registration time.
/// <see cref="Email"/> is stored normalized (trimmed, lower-cased) so that lookup agrees with the
/// filtered-unique index.
/// </summary>
public sealed class AdminInvitationEntity : IGuidEntity, IEventRaiser
{
    private AdminInvitationEntity() { }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public AdminInvitationStatus Status { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public Guid? AcceptedByUserId { get; private set; }
    public DateTime? AcceptedAt { get; private set; }

    private readonly EventRaiser events = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => events.DomainEvents;
    public void ClearDomainEvents() => events.Clear();

    /// <summary>Whether the invitation is still live at <paramref name="utcNow"/> — pending and unexpired. A lapsed
    /// row stays <see cref="AdminInvitationStatus.Pending"/> in storage (nothing sweeps it), so <c>Pending</c> alone
    /// is not "live".</summary>
    public bool IsActive(DateTime utcNow) => Status == AdminInvitationStatus.Pending && utcNow < ExpiresAt;

    public static AdminInvitationEntity Create(string email, Guid createdBy, DateTime at, TimeSpan ttl)
    {
        var invitation = new AdminInvitationEntity
        {
            Id = Guid.NewGuid(),
            Email = email,
            Status = AdminInvitationStatus.Pending,
            CreatedByUserId = createdBy,
            CreatedAt = at,
            ExpiresAt = at + ttl,
        };
        invitation.events.Raise(new AdminInvitationCreatedDomainEvent(invitation.Id, email));
        return invitation;
    }

    /// <summary>Accepts a still-pending, unexpired invitation for <paramref name="userId"/>.</summary>
    public UnitResult<AdminInvitationAcceptanceError> Accept(Guid userId, DateTime at)
    {
        if (Status != AdminInvitationStatus.Pending)
            return new AdminInvitationAcceptanceError.NotPending();
        if (at >= ExpiresAt)
            return new AdminInvitationAcceptanceError.Expired();
        Status = AdminInvitationStatus.Accepted;
        AcceptedByUserId = userId;
        AcceptedAt = at;
        return new Success();
    }

    /// <summary>Revokes a still-pending invitation.</summary>
    public UnitResult<AdminInvitationRevocationError> Revoke()
    {
        if (Status != AdminInvitationStatus.Pending)
            return new AdminInvitationRevocationError.NotPending();
        Status = AdminInvitationStatus.Revoked;
        return new Success();
    }

    /// <summary>Retires a lapsed invitation. The row stays <see cref="AdminInvitationStatus.Pending"/> once its TTL
    /// passes (nothing sweeps it), so a re-invite calls this to free the <c>Email</c> filtered-unique slot the
    /// stale row still occupies.</summary>
    public void Expire()
    {
        if (Status != AdminInvitationStatus.Pending)
            throw new DomainException("Only a pending invitation can expire.");
        Status = AdminInvitationStatus.Expired;
    }
}
