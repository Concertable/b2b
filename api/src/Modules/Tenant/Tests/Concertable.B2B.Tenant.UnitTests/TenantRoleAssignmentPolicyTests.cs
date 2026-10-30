using System.Collections.Immutable;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Domain;
using Concertable.B2B.Tenant.Domain.Entities;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class TenantRoleAssignmentPolicyTests
{
    [Theory]
    [InlineData("Staff", true)]
    [InlineData("Door", true)]
    [InlineData("Sound", true)]
    [InlineData("Finance", false)]
    [InlineData("Owner", false)]
    [InlineData("Manager", false)]
    public void CanAssign_Manager_AssignsOnlyInvitationAssignablePresets(string targetPreset, bool expected)
    {
        var tenantId = Guid.NewGuid();
        var actor = Snapshot(tenantId, "Manager");
        var role = Preset(tenantId, targetPreset);

        var result = TenantRoleAssignmentPolicy.CanAssign(actor, false, [role]);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CanAssign_ActorWithoutInvitePermission_DeniesAssignableRole()
    {
        var tenantId = Guid.NewGuid();
        var actor = Snapshot(tenantId, "Staff");
        var role = Preset(tenantId, "Door");

        var result = TenantRoleAssignmentPolicy.CanAssign(actor, false, [role]);

        Assert.False(result);
    }

    [Fact]
    public void CanAssign_ManagerCannotDelegateCustomInvitationAdministration()
    {
        var tenantId = Guid.NewGuid();
        var actor = Snapshot(tenantId, "Manager");
        var role = TenantRoleDefinition.CreateCustom(tenantId, "Inviter", true,
            new Dictionary<TenantPermission, ResourceAudience>
            {
                [TenantPermission.MembersInvite] = ResourceAudience.TenantResources,
            });

        Assert.False(TenantRoleAssignmentPolicy.CanAssign(actor, false, [role]));
        Assert.True(TenantRoleAssignmentPolicy.CanAssign(actor, true, [role]));
    }

    private static MembershipSnapshot Snapshot(Guid tenantId, string presetKey)
    {
        var preset = AuthorizationCatalog.Presets[presetKey];
        return new MembershipSnapshot(
            Guid.NewGuid(),
            tenantId,
            Guid.NewGuid(),
            1,
            1,
            preset.Permissions.ToImmutableDictionary());
    }

    private static TenantRoleDefinition Preset(Guid tenantId, string key)
    {
        var preset = AuthorizationCatalog.Presets[key];
        return TenantRoleDefinition.CreateSystemPreset(tenantId, key);
    }
}
