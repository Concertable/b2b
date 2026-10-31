using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Domain.Entities;

namespace Concertable.B2B.Tenant.UnitTests.Domain;

public sealed class ComposableRoleInvariantTests
{
    [Fact]
    public void RejectedRoleUpdatePreservesMetadataPermissionsAndVersion()
    {
        var role = TenantRoleDefinition.CreateCustom(Guid.NewGuid(), "Door crew", true,
            new Dictionary<TenantPermission, ResourceAccess>
            {
                [TenantPermission.MessagesRead] = ResourceAccess.Assigned,
            });
        var existingPermission = Assert.Single(role.Permissions);
        var version = role.Version;

        Assert.Throws<ArgumentException>(() => role.Update("Elevated crew", false,
            new Dictionary<TenantPermission, ResourceAccess>
            {
                [TenantPermission.MessagesRead] = ResourceAccess.All,
                [TenantPermission.MembersManageRoles] = ResourceAccess.All,
            }));

        Assert.Equal("Door crew", role.Name);
        Assert.True(role.IsInvitationAssignable);
        Assert.Equal(version, role.Version);
        Assert.Same(existingPermission, Assert.Single(role.Permissions));
        Assert.Equal(ResourceAccess.Assigned, existingPermission.Audience);
    }

    [Theory]
    [InlineData(ResourceAccess.None)]
    [InlineData((ResourceAccess)99)]
    public void CustomRoleRejectsAPermissionWithoutAccess(ResourceAccess access)
    {
        Assert.Throws<ArgumentException>(() => TenantRoleDefinition.CreateCustom(
            Guid.NewGuid(), "Door revenue", false,
            new Dictionary<TenantPermission, ResourceAccess>
            {
                [TenantPermission.ConcertsDeclareDoorRevenue] = access,
            }));
    }

    [Fact]
    public void ReplacingOneRolePreservesContinuousAssignmentAndAdvancesVersionOnce()
    {
        var tenantId = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var previous = Guid.NewGuid();
        var replacement = Guid.NewGuid();
        var initialIssuer = Guid.NewGuid();
        var nextIssuer = Guid.NewGuid();
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var changedAt = createdAt.AddDays(1);
        var membership = TenantMembershipEntity.Create(tenantId, Guid.NewGuid(),
            [owner, previous], initialIssuer, createdAt);
        var ownerAssignment = Assert.Single(membership.Assignments, row => row.RoleId == owner);

        Assert.True(membership.ChangeRoles(new HashSet<Guid> { owner, replacement }, nextIssuer, changedAt).IsSuccess);

        Assert.Equal(2, membership.PermissionVersion);
        Assert.Same(ownerAssignment, Assert.Single(membership.Assignments, row => row.RoleId == owner));
        Assert.Equal(initialIssuer, ownerAssignment.IssuedByMembershipId);
        Assert.Equal(createdAt, ownerAssignment.CreatedAt);
        var added = Assert.Single(membership.Assignments, row => row.RoleId == replacement);
        Assert.Equal(nextIssuer, added.IssuedByMembershipId);
        Assert.Equal(changedAt, added.CreatedAt);
        Assert.True(membership.ChangeRoles(new HashSet<Guid> { replacement, owner }, nextIssuer, changedAt).IsSuccess);
        Assert.Equal(2, membership.PermissionVersion);
    }

    [Fact]
    public void IndependentlyMaterializedSnapshotsComparePermissionValues()
    {
        var membershipId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var first = new MembershipSnapshot(membershipId, tenantId, userId, 2, 3,
            EffectivePermissions.From(
            [
                KeyValuePair.Create(TenantPermission.MessagesRead, ResourceAccess.Assigned),
                KeyValuePair.Create(TenantPermission.OperationsView, ResourceAccess.All),
            ]));
        var second = new MembershipSnapshot(membershipId, tenantId, userId, 2, 3,
            EffectivePermissions.From(
            [
                KeyValuePair.Create(TenantPermission.OperationsView, ResourceAccess.All),
                KeyValuePair.Create(TenantPermission.MessagesRead, ResourceAccess.Assigned),
            ]));

        Assert.Equal(first, second);
    }
}