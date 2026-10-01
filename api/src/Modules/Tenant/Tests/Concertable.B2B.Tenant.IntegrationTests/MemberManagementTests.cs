using System.Net;
using System.Net.Http.Json;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Contracts;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Tenant.IntegrationTests;

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

    private static Task<HttpResponseMessage> PutRole(HttpClient client, Guid tenantId, Guid userId, string presetKey) =>
        client.PutAsJsonAsync($"/api/organization/members/{userId}/roles", new { roleIds = new[] { TenantApiFixture.RoleId(tenantId, presetKey) } });

    // A member who owns another tenant must name the acting tenant explicitly, or resolution fails closed.
    private HttpClient ClientInTenant(Guid userId, string email, Guid tenantId)
    {
        var client = fixture.CreateClient(userId, email);
        client.DefaultRequestHeaders.Add(TenantHeaders.TenantId, tenantId.ToString());
        return client;
    }

    #region GetMembers

    [Fact]
    public async Task GetMembers_AsOwner_ReturnsAllMembersWithEmails()
    {
        var owner = fixture.SeedState.VenueManager1; // founding Owner, sole membership → default tenant
        var second = fixture.SeedState.VenueManagerNoVenue;
        await fixture.AddMembershipAsync(TenantOf(owner.Id), second.Id, "Staff");

        var response = await fixture.CreateClient(owner).GetAsync("/api/organization/members");

        await response.ShouldBe(HttpStatusCode.OK);
        var members = await response.Content.ReadAsync<List<MemberDto>>();
        Assert.Contains(members!, m => m.UserId == owner.Id && m.Email == owner.Email && m.Roles.Any(role => role.Name == "Owner"));
        Assert.Contains(members!, m => m.UserId == second.Id && m.Email == second.Email && m.Roles.Any(role => role.Name == "Staff"));
    }

    [Fact]
    public async Task GetMembers_AsManager_IsAllowed()
    {
        // Manager holds OperationsView, so viewing the roster is allowed (only mutations are Owner-gated).
        var manager = fixture.SeedState.VenueManagerNoVenue;
        var tenantId = TenantOf(fixture.SeedState.VenueManager1.Id);
        await fixture.AddMembershipAsync(tenantId, manager.Id, "Manager");

        var response = await ClientInTenant(manager.Id, manager.Email, tenantId).GetAsync("/api/organization/members");

        await response.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region ChangeRole

    [Fact]
    public async Task ChangeRole_AsOwner_UpdatesRole()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var member = fixture.SeedState.VenueManagerNoVenue;
        await fixture.AddMembershipAsync(tenantId, member.Id, "Staff");

        var response = await PutRole(fixture.CreateClient(owner), tenantId, member.Id, "Finance");

        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(TenantApiFixture.RoleId(tenantId, "Finance"), Assert.Single(fixture.Memberships.Single(m => m.TenantId == tenantId && m.UserId == member.Id).Assignments).RoleId);
    }

    [Fact]
    public async Task ChangeRole_AsManager_IsForbidden()
    {
        // Manager lacks MembersManageRoles — the mutation is refused before any service logic.
        var owner = fixture.SeedState.VenueManager1;
        var manager = fixture.SeedState.VenueManagerNoVenue;
        var tenantId = TenantOf(owner.Id);
        await fixture.AddMembershipAsync(tenantId, manager.Id, "Manager");

        var response = await PutRole(
            ClientInTenant(manager.Id, manager.Email, tenantId),
            tenantId,
            owner.Id,
            "Staff");

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ChangeRole_TargetIsNotAMember_IsNotFound()
    {
        var owner = fixture.SeedState.VenueManager1;

        var response = await PutRole(fixture.CreateClient(owner), TenantOf(owner.Id), fixture.SeedState.VenueManagerNoVenue.Id, "Manager");

        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChangeRole_DemotingSoleOwner_IsConflict()
    {
        var owner = fixture.SeedState.VenueManager1; // sole Owner of their tenant
        var tenantId = TenantOf(owner.Id);

        var response = await PutRole(fixture.CreateClient(owner), tenantId, owner.Id, "Manager");

        await response.ShouldBe(HttpStatusCode.Conflict);
        Assert.Equal(TenantApiFixture.RoleId(tenantId, "Owner"), Assert.Single(fixture.Memberships.Single(m => m.TenantId == tenantId && m.UserId == owner.Id).Assignments).RoleId);
    }

    #endregion

    #region RemoveMember

    [Fact]
    public async Task RemoveMember_AsOwner_RemovesMembership()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var member = fixture.SeedState.VenueManagerNoVenue;
        await fixture.AddMembershipAsync(tenantId, member.Id, "Staff");

        var response = await fixture.CreateClient(owner).DeleteAsync($"/api/organization/members/{member.Id}");

        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.DoesNotContain(fixture.Memberships, m => m.TenantId == tenantId && m.UserId == member.Id);
    }

    [Fact]
    public async Task RemoveMember_AsManager_IsForbidden()
    {
        var owner = fixture.SeedState.VenueManager1;
        var manager = fixture.SeedState.VenueManagerNoVenue;
        var tenantId = TenantOf(owner.Id);
        await fixture.AddMembershipAsync(tenantId, manager.Id, "Manager");

        var response = await ClientInTenant(manager.Id, manager.Email, tenantId)
            .DeleteAsync($"/api/organization/members/{owner.Id}");

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemoveMember_SoleOwnerSelfLeave_IsConflict()
    {
        var owner = fixture.SeedState.VenueManager1; // the only Owner
        var tenantId = TenantOf(owner.Id);

        var response = await fixture.CreateClient(owner).DeleteAsync($"/api/organization/members/{owner.Id}");

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

        var response = await fixture.CreateClient(owner).DeleteAsync($"/api/organization/members/{owner.Id}");

        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.DoesNotContain(fixture.Memberships, m => m.TenantId == tenantId && m.UserId == owner.Id);
        Assert.Contains(fixture.Memberships, m => m.TenantId == tenantId && m.UserId == coOwner.Id);
    }

    #endregion

    #region DeleteActiveTenant

    [Fact]
    public async Task DeleteTenant_AsOwner_DeletesTenantAndMemberships()
    {
        var owner = fixture.SeedState.VenueManagerNoVenue;
        var tenantId = TenantOf(owner.Id);
        await fixture.AddMembershipAsync(tenantId, fixture.SeedState.ArtistManagerNoArtist.Id, "Staff");

        var response = await fixture.CreateClient(owner).DeleteAsync("/api/organization");

        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.DoesNotContain(fixture.Tenants, t => t.Id == tenantId);
        Assert.DoesNotContain(fixture.Memberships, m => m.TenantId == tenantId);
    }

    [Fact]
    public async Task DeleteTenant_AsManager_IsForbidden()
    {
        var owner = fixture.SeedState.VenueManager1;
        var manager = fixture.SeedState.VenueManagerNoVenue;
        var tenantId = TenantOf(owner.Id);
        await fixture.AddMembershipAsync(tenantId, manager.Id, "Manager");

        var response = await ClientInTenant(manager.Id, manager.Email, tenantId)
            .DeleteAsync("/api/organization");

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Tenant-type-independent

    [Fact]
    public async Task Members_ArtistOwner_CanListAndManage()
    {
        var owner = fixture.SeedState.ArtistManager1; // founding Owner of an artist tenant
        var tenantId = TenantOf(owner.Id);
        var member = fixture.SeedState.ArtistManagerNoArtist;
        await fixture.AddMembershipAsync(tenantId, member.Id, "Staff");

        var list = await fixture.CreateClient(owner).GetAsync("/api/organization/members");
        await list.ShouldBe(HttpStatusCode.OK);
        Assert.Contains(await list.Content.ReadAsync<List<MemberDto>>() ?? [], m => m.UserId == member.Id);

        var promote = await PutRole(fixture.CreateClient(owner), tenantId, member.Id, "Manager");
        await promote.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(TenantApiFixture.RoleId(tenantId, "Manager"), Assert.Single(fixture.Memberships.Single(m => m.TenantId == tenantId && m.UserId == member.Id).Assignments).RoleId);
    }

    #endregion
}
