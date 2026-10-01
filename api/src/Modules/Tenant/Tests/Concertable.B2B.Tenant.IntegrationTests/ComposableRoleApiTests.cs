using System.Collections.Immutable;
using System.Net;
using Concertable.B2B.Authorization.Contracts;
using System.Net.Http.Json;
using System.Text.Json;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Tenant.Contracts;
using Xunit.Abstractions;

namespace Concertable.B2B.Tenant.IntegrationTests;

[Collection("Integration")]
public sealed class ComposableRoleApiTests : IAsyncLifetime
{
    private readonly TenantApiFixture fixture;

    public ComposableRoleApiTests(TenantApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    private HttpClient ClientInTenant(Guid userId, string email, Guid tenantId)
    {
        var client = fixture.CreateClient(userId, email);
        client.DefaultRequestHeaders.Add(TenantHeaders.TenantId, tenantId.ToString());
        return client;
    }

    private static async Task<Guid> CreateRoleAsync(HttpClient client, string name, object[] permissions)
    {
        var response = await client.PostAsJsonAsync("/api/organization/roles",
            new { name, isInvitationAssignable = true, permissions });
        await response.ShouldBe(HttpStatusCode.Created);
        var role = await response.Content.ReadFromJsonAsync<JsonElement>();
        return role.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task CustomOperationsAndFinanceComposeWithPerPermissionAudienceUnion()
    {
        var owner = fixture.SeedState.VenueManager1;
        var member = fixture.SeedState.VenueManagerNoVenue;
        var tenantId = fixture.SeedState.Tenants.Single(tenant => tenant.CreatedByUserId == owner.Id).Id;
        await fixture.AddMembershipAsync(tenantId, member.Id, "Staff");
        var ownerClient = fixture.CreateClient(owner);
        var customRoleId = await CreateRoleAsync(ownerClient, "Operations assistant",
            [new { permission = "operations.view", audience = "AssignedResources" },
             new { permission = "concerts.ops_edit", audience = "AssignedResources" }]);
        var assigned = await ownerClient.PutAsJsonAsync($"/api/organization/members/{member.Id}/roles",
            new { roleIds = new[] { customRoleId, TenantApiFixture.RoleId(tenantId, "Finance") } });
        await assigned.ShouldBe(HttpStatusCode.NoContent);

        var membership = fixture.Memberships.Single(value => value.TenantId == tenantId && value.UserId == member.Id);
        var tenant = fixture.Tenants.Single(value => value.Id == tenantId);
        var expectedPermissions = AuthorizationCatalog.Presets["Finance"].Permissions
            .ToImmutableDictionary()
            .SetItem(TenantPermission.ConcertsOpsEdit, ResourceAudience.AssignedResources);
        var expected = new MembershipSnapshot(membership.Id, tenantId, member.Id,
            membership.PermissionVersion, tenant.RolePolicyVersion, expectedPermissions);
        var resolved = await fixture.ExecuteResolutionAsync((resolver, ct) =>
            resolver.ResolveAsync(expected, tenantId, ct: ct));

        Assert.True(resolved.TryGetValue(out var resolution));
        Assert.Equal(ResourceAudience.TenantResources,
            resolution.Actor.AudienceFor(TenantPermission.OperationsView));
        Assert.Equal(ResourceAudience.AssignedResources,
            resolution.Actor.AudienceFor(TenantPermission.ConcertsOpsEdit));
        Assert.Equal(ResourceAudience.TenantResources,
            resolution.Actor.AudienceFor(TenantPermission.PayoutsManage));
        Assert.Equal(ResourceAudience.None,
            resolution.Actor.AudienceFor(TenantPermission.ConcertsManage));
    }

    [Fact]
    public async Task ZeroPermissionRoleKeepsCurrentMembershipVisible()
    {
        var owner = fixture.SeedState.VenueManager1;
        var member = fixture.SeedState.VenueManagerNoVenue;
        var tenantId = fixture.SeedState.Tenants.Single(tenant => tenant.CreatedByUserId == owner.Id).Id;
        await fixture.AddMembershipAsync(tenantId, member.Id, "Staff");
        var ownerClient = fixture.CreateClient(owner);
        var roleId = await CreateRoleAsync(ownerClient, "No access", []);

        var changed = await ownerClient.PutAsJsonAsync($"/api/organization/members/{member.Id}/roles",
            new { roleIds = new[] { roleId } });
        await changed.ShouldBe(HttpStatusCode.NoContent);

        var response = await ClientInTenant(member.Id, member.Email, tenantId).GetAsync("/api/auth/me");
        await response.ShouldBe(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<JsonElement>();
        var membership = user.GetProperty("memberships").EnumerateArray()
            .Single(value => value.GetProperty("tenantId").GetGuid() == tenantId);
        Assert.Empty(membership.GetProperty("permissions").EnumerateArray());
        Assert.Equal(roleId, Assert.Single(membership.GetProperty("roles").EnumerateArray())
            .GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task StaleRoleVersionCannotOverwriteCurrentDefinition()
    {
        var owner = fixture.SeedState.VenueManager1;
        var client = fixture.CreateClient(owner);
        var roleId = await CreateRoleAsync(client, "Draft operations", []);
        var changed = await client.PutAsJsonAsync($"/api/organization/roles/{roleId}",
            new { name = "Current operations", expectedVersion = 1,
                isInvitationAssignable = true, permissions = Array.Empty<object>() });
        await changed.ShouldBe(HttpStatusCode.OK);

        var stale = await client.PutAsJsonAsync($"/api/organization/roles/{roleId}",
            new { name = "Stale operations", expectedVersion = 1,
                isInvitationAssignable = true, permissions = Array.Empty<object>() });

        await stale.ShouldBe(HttpStatusCode.Conflict);
        var roles = await (await client.GetAsync("/api/organization/roles"))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Current operations", roles.EnumerateArray()
            .Single(value => value.GetProperty("id").GetGuid() == roleId)
            .GetProperty("name").GetString());
    }

    [Fact]
    public async Task ForeignTenantRoleCannotBeAssigned()
    {
        var firstOwner = fixture.SeedState.VenueManager1;
        var secondOwner = fixture.SeedState.VenueManager2;
        var secondTenantId = fixture.SeedState.Tenants.Single(tenant => tenant.CreatedByUserId == secondOwner.Id).Id;
        var member = fixture.SeedState.VenueManagerNoVenue;
        await fixture.AddMembershipAsync(secondTenantId, member.Id, "Staff");
        var foreignRoleId = await CreateRoleAsync(fixture.CreateClient(firstOwner), "Foreign role", []);

        var response = await fixture.CreateClient(secondOwner).PutAsJsonAsync(
            $"/api/organization/members/{member.Id}/roles", new { roleIds = new[] { foreignRoleId } });

        await response.ShouldBe(HttpStatusCode.BadRequest);
        Assert.Equal(TenantApiFixture.RoleId(secondTenantId, "Staff"),
            Assert.Single(fixture.Memberships.Single(value => value.TenantId == secondTenantId
                && value.UserId == member.Id).Assignments).RoleId);
    }

    [Fact]
    public async Task RetiringInUseRoleReplacesOnlyItsAssignment()
    {
        var owner = fixture.SeedState.VenueManager1;
        var member = fixture.SeedState.VenueManagerNoVenue;
        var tenantId = fixture.SeedState.Tenants.Single(tenant => tenant.CreatedByUserId == owner.Id).Id;
        await fixture.AddMembershipAsync(tenantId, member.Id, "Staff");
        var ownerClient = fixture.CreateClient(owner);
        var roleId = await CreateRoleAsync(ownerClient, "Messaging",
            [new { permission = "messages.read", audience = "AssignedResources" }]);
        var ownerRoleId = TenantApiFixture.RoleId(tenantId, "Owner");
        var assigned = await ownerClient.PutAsJsonAsync($"/api/organization/members/{member.Id}/roles",
            new { roleIds = new[] { ownerRoleId, roleId } });
        await assigned.ShouldBe(HttpStatusCode.NoContent);
        var before = fixture.Memberships.Single(value => value.TenantId == tenantId && value.UserId == member.Id);
        var ownerAssignment = before.Assignments.Single(value => value.RoleId == ownerRoleId);
        var oldVersion = before.PermissionVersion;

        var retired = await ownerClient.PostAsJsonAsync($"/api/organization/roles/{roleId}/retire",
            new { expectedVersion = 1, replacementRoleId = TenantApiFixture.RoleId(tenantId, "Staff") });

        await retired.ShouldBe(HttpStatusCode.NoContent);
        var after = fixture.Memberships.Single(value => value.TenantId == tenantId && value.UserId == member.Id);
        Assert.Equal(oldVersion + 1, after.PermissionVersion);
        Assert.Equal(ownerAssignment.CreatedAt,
            after.Assignments.Single(value => value.RoleId == ownerRoleId).CreatedAt);
        Assert.Equal(ownerAssignment.IssuedByMembershipId,
            after.Assignments.Single(value => value.RoleId == ownerRoleId).IssuedByMembershipId);
        Assert.Contains(after.Assignments, value => value.RoleId == TenantApiFixture.RoleId(tenantId, "Staff"));
        Assert.DoesNotContain(after.Assignments, value => value.RoleId == roleId);
    }
}