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
        var membership = fixture.Memberships.First(value => value.Role == TenantRole.Owner);
        var expected = Snapshot(membership);
        await fixture.ChangeMembershipRoleAsync(membership.TenantId, membership.UserId, TenantRole.Manager);
        var current = Snapshot(fixture.Memberships.Single(value => value.Id == membership.Id));

        var stale = await fixture.ExecuteResolutionAsync((resolver, ct) =>
            resolver.ResolveAsync(expected, expected.TenantId, ct: ct));
        var resolved = await fixture.ExecuteResolutionAsync((resolver, ct) =>
            resolver.ResolveAsync(current, current.TenantId, current.MembershipId, ct));

        Assert.Null(stale);
        Assert.NotEqual(expected.PermissionVersion, current.PermissionVersion);
        Assert.NotNull(resolved);
        Assert.Equal(current, resolved.Actor);
        Assert.Equal(current, resolved.TargetMembership);
        Assert.True(resolved.TargetTenantExists);
    }

    [Fact]
    public async Task Resolve_MembershipBelongsToAnotherTenant_ExcludesTargetMembership()
    {
        var actor = Snapshot(fixture.Memberships.First());
        var target = fixture.Memberships.First(value => value.TenantId != actor.TenantId);

        var resolved = await fixture.ExecuteResolutionAsync((resolver, ct) =>
            resolver.ResolveAsync(actor, actor.TenantId, target.Id, ct));

        Assert.NotNull(resolved);
        Assert.Equal(actor, resolved.Actor);
        Assert.True(resolved.TargetTenantExists);
        Assert.Null(resolved.TargetMembership);
    }

    [Fact]
    public async Task ResolveAudience_DuplicateAndMissingTenants_ReturnsUniqueExistingTenants()
    {
        var actor = Snapshot(fixture.Memberships.First());
        var otherTenantId = fixture.Tenants.First(value => value.Id != actor.TenantId).Id;
        Guid[] requested = [actor.TenantId, otherTenantId, actor.TenantId, Guid.NewGuid()];

        var resolved = await fixture.ExecuteResolutionAsync((resolver, ct) =>
            resolver.ResolveAudienceAsync(actor, requested, ct));

        Assert.NotNull(resolved);
        Assert.Equal(actor, resolved.Actor);
        Assert.True(resolved.ExistingTenantIds.SetEquals([actor.TenantId, otherTenantId]));
    }

    [Fact]
    public async Task ResolveAudience_ActorPermissionVersionChanged_RejectsStaleAuthority()
    {
        var membership = fixture.Memberships.First(value => value.Role == TenantRole.Owner);
        var expected = Snapshot(membership);
        await fixture.ChangeMembershipRoleAsync(membership.TenantId, membership.UserId, TenantRole.Manager);

        var resolved = await fixture.ExecuteResolutionAsync((resolver, ct) =>
            resolver.ResolveAudienceAsync(expected, [expected.TenantId], ct));

        Assert.Null(resolved);
    }

    private static MembershipSnapshot Snapshot(TenantMembershipEntity membership) =>
        new(membership.Id, membership.TenantId, membership.UserId, membership.Role, membership.PermissionVersion);
}
