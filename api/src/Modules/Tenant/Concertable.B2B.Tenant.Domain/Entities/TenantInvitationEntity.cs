using Concertable.B2B.Tenant.Domain.Events;
using Concertable.Kernel;
using Concertable.B2B.Tenant.Domain.Errors;

namespace Concertable.B2B.Tenant.Domain.Entities;

public sealed class TenantInvitationEntity : IGuidEntity, IEventRaiser
{
    private TenantInvitationEntity() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    public string Email { get; private set; } = null!;
    public TenantRole Role { get; private set; }
    public InvitationStatus Status { get; private set; }
    public Guid InviterMembershipId { get; private set; }
    public long InviterPermissionVersion { get; private set; }
    public long Version { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public Guid? AcceptedByUserId { get; private set; }
    public DateTime? AcceptedAt { get; private set; }

    private readonly EventRaiser events = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => events.DomainEvents;
    public void ClearDomainEvents() => events.Clear();

    public bool IsActive(DateTime utcNow) => Status == InvitationStatus.Pending && utcNow < ExpiresAt;

    public static TenantInvitationEntity Create(
        Guid tenantId,
        string email,
        TenantRole role,
        Guid inviterMembershipId,
        long inviterPermissionVersion,
        DateTime at,
        TimeSpan ttl)
    {
        var invitation = new TenantInvitationEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            Role = role,
            Status = InvitationStatus.Pending,
            InviterMembershipId = inviterMembershipId,
            InviterPermissionVersion = inviterPermissionVersion,
            Version = 1,
            CreatedAt = at,
            ExpiresAt = at + ttl,
        };
        invitation.events.Raise(new TenantInvitationCreatedDomainEvent(invitation.Id, email, role));
        return invitation;
    }

    public UnitResult<InvitationAcceptanceError> Accept(Guid userId, DateTime at)
    {
        if (Status != InvitationStatus.Pending)
            return new InvitationAcceptanceError.NotPending();
        if (at >= ExpiresAt)
            return new InvitationAcceptanceError.Expired();
        Status = InvitationStatus.Accepted;
        AcceptedByUserId = userId;
        AcceptedAt = at;
        Version++;
        return new Success();
    }

    public UnitResult<InvitationRevocationError> Revoke()
    {
        if (Status != InvitationStatus.Pending)
            return new InvitationRevocationError.NotPending();
        Status = InvitationStatus.Revoked;
        Version++;
        return new Success();
    }

    public void Expire()
    {
        if (Status != InvitationStatus.Pending)
            throw new DomainException("Only a pending invitation can expire.");
        Status = InvitationStatus.Expired;
        Version++;
    }
}
