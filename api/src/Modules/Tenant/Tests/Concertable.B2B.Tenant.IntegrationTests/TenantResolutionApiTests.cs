using System.Collections.Immutable;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain.Entities;
using Xunit.Abstractions;

namespace Concertable.B2B.Tenant.IntegrationTests;

[Collection("Integration")]
public sealed class TenantResolutionApiTests : IAsyncLifetime
{
    private readonly TenantApiFixture fixture;

    public TenantResolutionApiTests(TenantApiFixture fixture, ITestOutputHelper output)
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
    public async Task Resolve_RoleChanged_RejectsOldAuthorityAndAcceptsCurrentSnapshot()
    {
        var membership = fixture.Memberships.First(value => HasRole(value, "Owner"));
        var expected = Snapshot(membership);
        await fixture.ChangeMembershipRoleAsync(membership.TenantId, membership.UserId, "Manager");
        var current = Snapshot(fixture.Memberships.Single(value => value.Id == membership.Id));

        var stale = await fixture.ExecuteResolutionAsync((resolver, ct) =>
            resolver.ResolveAsync(expected, expected.TenantId, ct: ct));
        var resolved = await fixture.ExecuteResolutionAsync((resolver, ct) =>
            resolver.ResolveAsync(current, current.TenantId, current.MembershipId, ct));

        Assert.True(stale.IsNone);
        Assert.NotEqual(expected.PermissionVersion, current.PermissionVersion);
        Assert.True(resolved.TryGetValue(out var resolution));
        Assert.True(current.HasSameAuthorityAs(resolution.Actor));
        Assert.True(current.HasSameAuthorityAs(resolution.TargetMembership));
        Assert.True(resolution.TargetTenantExists);
    }

    [Fact]
    public async Task Resolve_MembershipBelongsToAnotherTenant_ExcludesTargetMembership()
    {
        var actor = Snapshot(fixture.Memberships.First());
        var target = fixture.Memberships.First(value => value.TenantId != actor.TenantId);

        var resolved = await fixture.ExecuteResolutionAsync((resolver, ct) =>
            resolver.ResolveAsync(actor, actor.TenantId, target.Id, ct));

        Assert.True(resolved.TryGetValue(out var resolution));
        Assert.True(actor.HasSameAuthorityAs(resolution.Actor));
        Assert.True(resolution.TargetTenantExists);
        Assert.Null(resolution.TargetMembership);
    }

    [Fact]
    public async Task ResolveMany_ActorTenantOutsideTargetsStillResolvesWithoutReturningActorTenant()
    {
        var actor = Snapshot(fixture.Memberships.First());
        var targetTenantId = fixture.Tenants.First(tenant => tenant.Id != actor.TenantId).Id;

        var resolved = await fixture.ExecuteResolutionAsync((resolver, ct) =>
            resolver.ResolveManyAsync(actor, [targetTenantId], ct));

        Assert.True(resolved.TryGetValue(out var resolution));
        Assert.True(actor.HasSameAuthorityAs(resolution.Actor));
        Assert.True(resolution.ExistingTenantIds.SetEquals([targetTenantId]));
    }

    [Fact]
    public async Task ResolveMany_DuplicateAndMissingTenants_ReturnsUniqueExistingTenants()
    {
        var actor = Snapshot(fixture.Memberships.First());
        var otherTenantId = fixture.Tenants.First(value => value.Id != actor.TenantId).Id;
        Guid[] requested = [actor.TenantId, otherTenantId, actor.TenantId, Guid.NewGuid()];

        var resolved = await fixture.ExecuteResolutionAsync((resolver, ct) =>
            resolver.ResolveManyAsync(actor, requested, ct));

        Assert.True(resolved.TryGetValue(out var resolution));
        Assert.True(actor.HasSameAuthorityAs(resolution.Actor));
        Assert.True(resolution.ExistingTenantIds.SetEquals([actor.TenantId, otherTenantId]));
    }

    [Fact]
    public async Task ResolveMany_ActorPermissionVersionChanged_RejectsStaleAuthority()
    {
        var membership = fixture.Memberships.First(value => HasRole(value, "Owner"));
        var expected = Snapshot(membership);
        await fixture.ChangeMembershipRoleAsync(membership.TenantId, membership.UserId, "Manager");

        var resolved = await fixture.ExecuteResolutionAsync((resolver, ct) =>
            resolver.ResolveManyAsync(expected, [expected.TenantId], ct));

        Assert.True(resolved.IsNone);
    }

    private MembershipSnapshot Snapshot(TenantMembershipEntity membership)
    {
        var tenant = fixture.Tenants.Single(value => value.Id == membership.TenantId);
        var presetKeys = AuthorizationCatalog.Presets.Keys.Where(key =>
            membership.Assignments.Any(assignment => assignment.RoleId == TenantApiFixture.RoleId(membership.TenantId, key)));
        var permissions = presetKeys
            .SelectMany(key => AuthorizationCatalog.Presets[key].Permissions)
            .GroupBy(permission => permission.Key)
            .ToImmutableDictionary(group => group.Key, group => group.Max(permission => permission.Value));
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
