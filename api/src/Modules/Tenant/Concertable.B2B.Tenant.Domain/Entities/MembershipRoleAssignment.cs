namespace Concertable.B2B.Tenant.Domain.Entities;

public sealed class MembershipRoleAssignment
{
    private MembershipRoleAssignment() { }

    public Guid TenantId { get; private set; }
    public Guid MembershipId { get; private set; }
    public Guid RoleId { get; private set; }
    public Guid? IssuedByMembershipId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    internal static MembershipRoleAssignment Create(
        Guid tenantId, Guid membershipId, Guid roleId, Guid? issuedByMembershipId, DateTime at) =>
        new()
        {
            TenantId = tenantId,
            MembershipId = membershipId,
            RoleId = roleId,
            IssuedByMembershipId = issuedByMembershipId,
            CreatedAt = at,
        };
}
