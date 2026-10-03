using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain.Entities;

public sealed class TenantMembershipEntity : IGuidEntity
{
    private TenantMembershipEntity() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public long PermissionVersion { get; private set; }
    public Guid? InvitedByMembershipId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<MembershipRoleAssignment> assignments = [];
    public IReadOnlyList<MembershipRoleAssignment> Assignments => assignments.AsReadOnly();

    public static TenantMembershipEntity Create(
        Guid tenantId, Guid userId, IReadOnlyCollection<Guid> roleIds, Guid? invitedBy, DateTime at)
    {
        if (tenantId == Guid.Empty || userId == Guid.Empty || roleIds.Count == 0
            || roleIds.Any(id => id == Guid.Empty) || roleIds.Count != roleIds.Distinct().Count())
            throw new ArgumentException("Invalid membership role assignment.");
        var membership = new TenantMembershipEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            PermissionVersion = 1,
            InvitedByMembershipId = invitedBy,
            CreatedAt = at,
        };
        membership.assignments.AddRange(roleIds.Select(id =>
            MembershipRoleAssignment.Create(tenantId, membership.Id, id, invitedBy, at)));
        return membership;
    }

    public void ReplaceRoles(IReadOnlyCollection<Guid> roleIds, Guid issuerMembershipId, DateTime at)
    {
        if (roleIds.Count == 0 || roleIds.Any(id => id == Guid.Empty)
            || roleIds.Count != roleIds.Distinct().Count())
            throw new ArgumentException("Invalid membership role assignment.");
        if (assignments.Select(a => a.RoleId).ToHashSet().SetEquals(roleIds))
            return;
        assignments.RemoveAll(row => !roleIds.Contains(row.RoleId));
        var existing = assignments.Select(row => row.RoleId).ToHashSet();
        assignments.AddRange(roleIds.Where(id => !existing.Contains(id)).Select(id =>
            MembershipRoleAssignment.Create(TenantId, Id, id, issuerMembershipId, at)));
        PermissionVersion++;
    }
}
