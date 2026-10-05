namespace Concertable.B2B.Tenant.Domain.Entities;

public sealed class InvitationRoleAssignment
{
    private InvitationRoleAssignment() { }

    public Guid TenantId { get; private set; }
    public Guid InvitationId { get; private set; }
    public Guid RoleId { get; private set; }

    internal static InvitationRoleAssignment Create(Guid tenantId, Guid invitationId, Guid roleId) =>
        new() { TenantId = tenantId, InvitationId = invitationId, RoleId = roleId };
}
