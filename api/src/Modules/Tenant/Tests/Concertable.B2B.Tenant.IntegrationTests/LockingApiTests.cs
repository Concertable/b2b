using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain.Entities;
using Xunit.Abstractions;

namespace Concertable.B2B.Tenant.IntegrationTests;

[Collection("Integration")]
public sealed class LockingApiTests : IAsyncLifetime
{
    private readonly TenantApiFixture fixture;

    public LockingApiTests(TenantApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync()
    {
        fixture.DetachOutput();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task LockMemberships_RoleChanged_RejectsOldAuthorityAndAcceptsCurrentSnapshot()
    {
        var membership = fixture.Memberships.AsEnumerable().First(value => HasRole(value, "Owner"));
        var expected = Snapshot(membership);
        await fixture.ChangeMembershipRolesAsync(membership.TenantId, membership.UserId, "Manager");
        var current = Snapshot(fixture.Memberships.Single(value => value.Id == membership.Id));

        var stale = await fixture.ExecuteLockAsync((IMembershipLocker locker, CancellationToken ct) =>
            locker.LockAsync(expected, expected.TenantId, ct: ct));
        var locked = await fixture.ExecuteLockAsync((IMembershipLocker locker, CancellationToken ct) =>
            locker.LockAsync(current, current.TenantId, current.MembershipId, ct));

        Assert.True(stale.IsNone);
        Assert.NotEqual(expected.PermissionVersion, current.PermissionVersion);
        Assert.True(locked.TryGetValue(out var memberships));
        Assert.Equal(current, memberships.Actor);
        Assert.Equal(current, memberships.Target);
    }

    [Fact]
    public async Task LockMemberships_TargetBelongsToAnotherTenant_ExcludesTarget()
    {
        var actor = Snapshot(fixture.Memberships.First());
        var target = fixture.Memberships.First(value => value.TenantId != actor.TenantId);

        var locked = await fixture.ExecuteLockAsync((IMembershipLocker locker, CancellationToken ct) =>
            locker.LockAsync(actor, actor.TenantId, target.Id, ct));

        Assert.True(locked.TryGetValue(out var memberships));
        Assert.Equal(actor, memberships.Actor);
        Assert.Null(memberships.Target);
    }

    [Fact]
    public async Task LockExistingTenants_ReturnsOnlyTheRequestedTenants()
    {
        var actor = Snapshot(fixture.Memberships.First());
        var targetTenantId = fixture.Tenants.First(tenant => tenant.Id != actor.TenantId).Id;

        var existing = await fixture.ExecuteLockAsync((ITenantLocker locker, CancellationToken ct) =>
            locker.LockExistingAsync([targetTenantId], ct));

        Assert.True(existing.SetEquals([targetTenantId]));
    }

    [Fact]
    public async Task LockExistingTenants_DuplicateAndMissingTenants_ReturnsUniqueExistingTenants()
    {
        var actor = Snapshot(fixture.Memberships.First());
        var otherTenantId = fixture.Tenants.First(value => value.Id != actor.TenantId).Id;
        Guid[] requested = [actor.TenantId, otherTenantId, actor.TenantId, Guid.NewGuid()];

        var existing = await fixture.ExecuteLockAsync((ITenantLocker locker, CancellationToken ct) =>
            locker.LockExistingAsync(requested, ct));

        Assert.True(existing.SetEquals([actor.TenantId, otherTenantId]));
    }

    private MembershipSnapshot Snapshot(TenantMembershipEntity membership)
    {
        var tenant = fixture.Tenants.Single(value => value.Id == membership.TenantId);
        var presetKeys = AuthorizationCatalog.Presets.Keys.Where(key =>
            membership.Assignments.Any(assignment => assignment.RoleId == TenantApiFixture.RoleId(membership.TenantId, key)));
        var permissions = PermissionSet.From(
            presetKeys.SelectMany(key => AuthorizationCatalog.Presets[key].Permissions));
        return new MembershipSnapshot(
            membership.Id,
            membership.TenantId,
            membership.UserId,
            membership.PermissionVersion,
            tenant.RolePolicyVersion,
            permissions);
    }

    private static bool HasRole(TenantMembershipEntity membership, string presetKey) =>
        membership.Assignments.Any(assignment => assignment.RoleId == TenantApiFixture.RoleId(membership.TenantId, presetKey));
}
