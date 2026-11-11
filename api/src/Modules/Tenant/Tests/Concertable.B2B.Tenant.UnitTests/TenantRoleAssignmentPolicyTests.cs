using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain;
using Concertable.B2B.Tenant.Domain.Entities;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class TenantRoleAssignmentPolicyTests
{
    [Theory]
    [InlineData(SystemRole.Staff, true)]
    [InlineData(SystemRole.Door, true)]
    [InlineData(SystemRole.Sound, true)]
    [InlineData(SystemRole.Finance, false)]
    [InlineData(SystemRole.Owner, false)]
    [InlineData(SystemRole.Manager, false)]
    public void CanAssign_Manager_AssignsOnlyInvitationAssignablePresets(SystemRole targetRole, bool expected)
    {
        var tenantId = Guid.NewGuid();
        var actor = Snapshot(tenantId, SystemRole.Manager);
        var role = Preset(tenantId, targetRole);

        var result = TenantRoleAssignmentPolicy.CanAssign(actor, false, [role]);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CanAssign_ActorWithoutInvitePermission_DeniesAssignableRole()
    {
        var tenantId = Guid.NewGuid();
        var actor = Snapshot(tenantId, SystemRole.Staff);
        var role = Preset(tenantId, SystemRole.Door);

        var result = TenantRoleAssignmentPolicy.CanAssign(actor, false, [role]);

        Assert.False(result);
    }

    [Fact]
    public void CanAssign_ManagerCannotDelegateCustomInvitationAdministration()
    {
        var tenantId = Guid.NewGuid();
        var actor = Snapshot(tenantId, SystemRole.Manager);
        var role = TenantRoleDefinition.CreateCustom(tenantId, "Inviter", true,
            new Dictionary<TenantPermission, ResourceAccess>
            {
                [TenantPermission.MembersInvite] = ResourceAccess.All,
            });

        Assert.False(TenantRoleAssignmentPolicy.CanAssign(actor, false, [role]));
        Assert.True(TenantRoleAssignmentPolicy.CanAssign(actor, true, [role]));
    }

    private static MembershipSnapshot Snapshot(Guid tenantId, SystemRole role) =>
        new(
            Guid.NewGuid(),
            tenantId,
            Guid.NewGuid(),
            1,
            1,
            PermissionSet.From(role.Grants));

    private static TenantRoleDefinition Preset(Guid tenantId, SystemRole role) =>
        TenantRoleDefinition.CreateSystemRole(tenantId, role);
}
