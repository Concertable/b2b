using System.Net;
using System.Net.Http.Json;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.User.Domain.Entities;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Tenant.IntegrationTests;

/// <summary>
/// Phase 6.2 member management on <c>api/organizations</c> — list/change-role/remove + delete-org — through the
/// real ASP.NET pipeline. Covers the permission matrix boundaries (Owner vs Manager), the service-layer
/// last-Owner invariant (demote/remove/self-leave), and that the surface is persona-agnostic (venue + artist).
/// A founding Owner acting in their own single tenant resolves it by default (no header). When a second operator
/// is added as a member, that operator already owns a tenant too, so it must name the acting tenant via the
/// <c>X-Tenant-Id</c> header — otherwise resolution fails closed (403) and a permission test would pass for the
/// wrong reason.
/// </summary>
[Collection("Integration")]
public sealed class MemberManagementTests : IAsyncLifetime
{
    private readonly TenantApiFixture fixture;

    public MemberManagementTests(TenantApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    private Guid TenantOf(Guid userId) => fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == userId).Id;

    private static Task<HttpResponseMessage> PutRole(HttpClient client, Guid userId, TenantRole role) =>
        client.PutAsJsonAsync($"/api/organizations/members/{userId}/role", new { role = role.ToString() });

    // A member who owns another tenant must name the acting tenant explicitly, or resolution fails closed.
    private HttpClient ClientInTenant(UserEntity user, Guid tenantId)
    {
        var client = fixture.CreateClient(user);
        client.DefaultRequestHeaders.Add(TenantHeaders.TenantId, tenantId.ToString());
        return client;
    }

    // ---- GET members (OperationsView: Owner + Manager) ----

    [Fact]
    public async Task GetMembers_AsOwner_ReturnsAllMembersWithEmails()
    {
        var owner = fixture.SeedState.VenueManager1; // founding Owner, sole membership → default tenant
        var second = fixture.SeedState.VenueManagerNoVenue;
        await fixture.AddMembershipAsync(TenantOf(owner.Id), second.Id, TenantRole.Staff);

        var response = await fixture.CreateClient(owner).GetAsync("/api/organizations/members");

        await response.ShouldBe(HttpStatusCode.OK);
        var members = await response.Content.ReadAsync<List<MemberDto>>();
        Assert.Contains(members!, m => m.UserId == owner.Id && m.Email == owner.Email && m.Role == TenantRole.Owner);
        Assert.Contains(members!, m => m.UserId == second.Id && m.Email == second.Email && m.Role == TenantRole.Staff);
    }

    [Fact]
    public async Task GetMembers_AsManager_IsAllowed()
    {
        // Manager holds OperationsView, so viewing the roster is allowed (only mutations are Owner-gated).
        var manager = fixture.SeedState.VenueManagerNoVenue;
        var tenantId = TenantOf(fixture.SeedState.VenueManager1.Id);
        await fixture.AddMembershipAsync(tenantId, manager.Id, TenantRole.Manager);

        var response = await ClientInTenant(manager, tenantId).GetAsync("/api/organizations/members");

        await response.ShouldBe(HttpStatusCode.OK);
    }

    // ---- PUT members/{id}/role (MembersManageRoles: Owner only) ----

    [Fact]
    public async Task ChangeRole_AsOwner_UpdatesRole()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var member = fixture.SeedState.VenueManagerNoVenue;
        await fixture.AddMembershipAsync(tenantId, member.Id, TenantRole.Staff);

        var response = await PutRole(fixture.CreateClient(owner), member.Id, TenantRole.Finance);

        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(TenantRole.Finance, fixture.Memberships.Single(m => m.TenantId == tenantId && m.UserId == member.Id).Role);
    }

    [Fact]
    public async Task ChangeRole_AsManager_IsForbidden()
    {
        // Manager lacks MembersManageRoles — the mutation is refused before any service logic.
        var owner = fixture.SeedState.VenueManager1;
        var manager = fixture.SeedState.VenueManagerNoVenue;
        var tenantId = TenantOf(owner.Id);
        await fixture.AddMembershipAsync(tenantId, manager.Id, TenantRole.Manager);

        var response = await PutRole(ClientInTenant(manager, tenantId), owner.Id, TenantRole.Staff);

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ChangeRole_TargetIsNotAMember_IsNotFound()
    {
        var owner = fixture.SeedState.VenueManager1;

        var response = await PutRole(fixture.CreateClient(owner), fixture.SeedState.VenueManagerNoVenue.Id, TenantRole.Manager);

        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChangeRole_DemotingSoleOwner_IsConflict()
    {
        var owner = fixture.SeedState.VenueManager1; // sole Owner of their tenant
        var tenantId = TenantOf(owner.Id);

        var response = await PutRole(fixture.CreateClient(owner), owner.Id, TenantRole.Manager);

        await response.ShouldBe(HttpStatusCode.Conflict);
        Assert.Equal(TenantRole.Owner, fixture.Memberships.Single(m => m.TenantId == tenantId && m.UserId == owner.Id).Role);
    }

    // ---- DELETE members/{id} (MembersRemove: Owner only) + last-Owner + self-leave ----

    [Fact]
    public async Task RemoveMember_AsOwner_RemovesMembership()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var member = fixture.SeedState.VenueManagerNoVenue;
        await fixture.AddMembershipAsync(tenantId, member.Id, TenantRole.Staff);

        var response = await fixture.CreateClient(owner).DeleteAsync($"/api/organizations/members/{member.Id}");

        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.DoesNotContain(fixture.Memberships, m => m.TenantId == tenantId && m.UserId == member.Id);
    }

    [Fact]
    public async Task RemoveMember_AsManager_IsForbidden()
    {
        var owner = fixture.SeedState.VenueManager1;
        var manager = fixture.SeedState.VenueManagerNoVenue;
        var tenantId = TenantOf(owner.Id);
        await fixture.AddMembershipAsync(tenantId, manager.Id, TenantRole.Manager);

        var response = await ClientInTenant(manager, tenantId).DeleteAsync($"/api/organizations/members/{owner.Id}");

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemoveMember_SoleOwnerSelfLeave_IsConflict()
    {
        var owner = fixture.SeedState.VenueManager1; // the only Owner
        var tenantId = TenantOf(owner.Id);

        var response = await fixture.CreateClient(owner).DeleteAsync($"/api/organizations/members/{owner.Id}");

        await response.ShouldBe(HttpStatusCode.Conflict);
        Assert.Contains(fixture.Memberships, m => m.TenantId == tenantId && m.UserId == owner.Id);
    }

    [Fact]
    public async Task RemoveMember_NonSoleOwnerSelfLeave_Succeeds()
    {
        // Two Owners → an Owner may leave (self-leave allowed unless sole Owner); the tenant keeps an Owner.
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var coOwner = fixture.SeedState.VenueManagerNoVenue;
        await fixture.AddOwnerMembershipAsync(tenantId, coOwner.Id);

        var response = await fixture.CreateClient(owner).DeleteAsync($"/api/organizations/members/{owner.Id}");

        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.DoesNotContain(fixture.Memberships, m => m.TenantId == tenantId && m.UserId == owner.Id);
        Assert.Contains(fixture.Memberships, m => m.TenantId == tenantId && m.UserId == coOwner.Id);
    }

    // ---- DELETE api/organizations (TenantDelete: Owner only) ----

    [Fact]
    public async Task DeleteOrganization_AsOwner_DeletesTenantAndMemberships()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        await fixture.AddMembershipAsync(tenantId, fixture.SeedState.VenueManagerNoVenue.Id, TenantRole.Staff);

        var response = await fixture.CreateClient(owner).DeleteAsync("/api/organizations");

        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.DoesNotContain(fixture.Tenants, t => t.Id == tenantId);
        Assert.DoesNotContain(fixture.Memberships, m => m.TenantId == tenantId);
    }

    [Fact]
    public async Task DeleteOrganization_AsManager_IsForbidden()
    {
        var owner = fixture.SeedState.VenueManager1;
        var manager = fixture.SeedState.VenueManagerNoVenue;
        var tenantId = TenantOf(owner.Id);
        await fixture.AddMembershipAsync(tenantId, manager.Id, TenantRole.Manager);

        var response = await ClientInTenant(manager, tenantId).DeleteAsync("/api/organizations");

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ---- Persona-agnostic: the same surface serves artist tenants ----

    [Fact]
    public async Task Members_ArtistOwner_CanListAndManage()
    {
        var owner = fixture.SeedState.ArtistManager1; // founding Owner of an artist tenant
        var tenantId = TenantOf(owner.Id);
        var member = fixture.SeedState.ArtistManagerNoArtist;
        await fixture.AddMembershipAsync(tenantId, member.Id, TenantRole.Staff);

        var list = await fixture.CreateClient(owner).GetAsync("/api/organizations/members");
        await list.ShouldBe(HttpStatusCode.OK);
        Assert.Contains(await list.Content.ReadAsync<List<MemberDto>>() ?? [], m => m.UserId == member.Id);

        var promote = await PutRole(fixture.CreateClient(owner), member.Id, TenantRole.Manager);
        await promote.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(TenantRole.Manager, fixture.Memberships.Single(m => m.TenantId == tenantId && m.UserId == member.Id).Role);
    }
}
