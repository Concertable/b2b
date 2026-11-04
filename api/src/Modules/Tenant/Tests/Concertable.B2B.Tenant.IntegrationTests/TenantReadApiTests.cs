using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain.Entities;
using Xunit.Abstractions;

namespace Concertable.B2B.Tenant.IntegrationTests;

[Collection("Integration")]
public sealed class TenantReadApiTests : IAsyncLifetime
{
    private readonly TenantApiFixture fixture;

    public TenantReadApiTests(TenantApiFixture fixture, ITestOutputHelper output)
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
    public async Task GetCurrentMembership_RoleChanged_ReturnsTheCurrentSnapshot()
    {
        var membership = fixture.Memberships.AsEnumerable().First(value => HasRole(value, "Owner"));
        var before = Snapshot(membership);
        await fixture.ChangeMembershipRolesAsync(membership.TenantId, membership.UserId, "Manager");
        var current = Snapshot(fixture.Memberships.Single(value => value.Id == membership.Id));

        var read = await fixture.RunInTransactionAsync((ITenantModule tenantModule, CancellationToken ct) =>
            tenantModule.GetCurrentMembershipAsync(membership.TenantId, membership.Id, ct));

        Assert.NotEqual(before.PermissionVersion, current.PermissionVersion);
        Assert.True(read.TryGetValue(out var snapshot));
        Assert.Equal(current, snapshot);
    }

    [Fact]
    public async Task GetCurrentMembership_MembershipOfAnotherTenant_IsNone()
    {
        var tenantId = fixture.Memberships.First().TenantId;
        var other = fixture.Memberships.First(value => value.TenantId != tenantId);

        var read = await fixture.RunInTransactionAsync((ITenantModule tenantModule, CancellationToken ct) =>
            tenantModule.GetCurrentMembershipAsync(tenantId, other.Id, ct));

        Assert.True(read.IsNone);
    }

    [Fact]
    public async Task ExistsByIdForKeyShareAsync_SeededTenant_IsTrue()
    {
        var tenantId = fixture.Tenants.First().Id;

        var exists = await fixture.RunInTransactionAsync((ITenantRepository tenantRepository, CancellationToken ct) =>
            tenantRepository.ExistsByIdWithKeyShareLockAsync(tenantId, ct));

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsByIdForKeyShareAsync_UnknownTenant_IsFalse()
    {
        var exists = await fixture.RunInTransactionAsync((ITenantRepository tenantRepository, CancellationToken ct) =>
            tenantRepository.ExistsByIdWithKeyShareLockAsync(Guid.NewGuid(), ct));

        Assert.False(exists);
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
