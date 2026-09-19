using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain.Entities;

public sealed class TenantMembershipEntity : IGuidEntity
{
    private TenantMembershipEntity() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }
    public TenantRole Role { get; private set; }

    public long PermissionVersion { get; private set; }

    public Guid? InvitedByMembershipId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static TenantMembershipEntity Create(Guid tenantId, Guid userId, TenantRole role, Guid? invitedBy, DateTime at) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Role = role,
            PermissionVersion = 1,
            InvitedByMembershipId = invitedBy,
            CreatedAt = at,
        };

    public void ChangeRole(TenantRole role)
    {
        Role = role;
        PermissionVersion++;
    }
}
