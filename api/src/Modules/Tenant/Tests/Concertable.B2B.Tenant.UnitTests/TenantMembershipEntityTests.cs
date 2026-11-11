using Concertable.B2B.Tenant.Domain.Entities;
using Concertable.B2B.Tenant.Domain.Errors;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class TenantMembershipEntityTests
{
    [Fact]
    public void Create_FoundingOwner_RecordsAssignedPresetAndNoInviter()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ownerRoleId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var membership = TenantMembershipEntity.Create(tenantId, userId, [ownerRoleId], invitedBy: null, now);

        Assert.NotEqual(Guid.Empty, membership.Id);
        Assert.Equal(tenantId, membership.TenantId);
        Assert.Equal(userId, membership.UserId);
        Assert.Equal(ownerRoleId, Assert.Single(membership.Assignments).RoleId);
        Assert.Null(membership.InvitedByMembershipId);
        Assert.Equal(now, membership.CreatedAt);
    }

    [Fact]
    public void Create_Invited_RecordsIssuerOnAssignments()
    {
        var tenantId = Guid.NewGuid();
        var invitedBy = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var membership = TenantMembershipEntity.Create(tenantId, Guid.NewGuid(), [roleId], invitedBy, DateTime.UtcNow);

        Assert.Equal(invitedBy, membership.InvitedByMembershipId);
        Assert.Equal(invitedBy, Assert.Single(membership.Assignments).IssuedByMembershipId);
    }

    [Fact]
    public void ChangeRoles_ChangedAssignments_IncrementsPermissionVersion()
    {
        var tenantId = Guid.NewGuid();
        var membership = TenantMembershipEntity.Create(tenantId, Guid.NewGuid(), [Guid.NewGuid()], null, DateTime.UtcNow);

        var result = membership.ChangeRoles(
            new HashSet<Guid> { Guid.NewGuid(), Guid.NewGuid() }, Guid.NewGuid(), DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, membership.PermissionVersion);
        Assert.Equal(2, membership.Assignments.Count);
    }

    [Fact]
    public void ChangeRoles_NoRoles_IsRejectedAndLeavesTheAssignmentsUnchanged()
    {
        var roleId = Guid.NewGuid();
        var membership = TenantMembershipEntity.Create(Guid.NewGuid(), Guid.NewGuid(), [roleId], null, DateTime.UtcNow);

        var result = membership.ChangeRoles(new HashSet<Guid>(), Guid.NewGuid(), DateTime.UtcNow);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<MembershipRoleError.NoRoles>(error);
        Assert.Equal(1, membership.PermissionVersion);
        Assert.Equal(roleId, Assert.Single(membership.Assignments).RoleId);
    }

    [Fact]
    public void ChangeRoles_SameRoles_SucceedsWithoutANewVersion()
    {
        var roleId = Guid.NewGuid();
        var membership = TenantMembershipEntity.Create(Guid.NewGuid(), Guid.NewGuid(), [roleId], null, DateTime.UtcNow);

        var result = membership.ChangeRoles(new HashSet<Guid> { roleId }, Guid.NewGuid(), DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, membership.PermissionVersion);
    }
}
