using System.Net;
using System.Net.Http.Json;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;
using Xunit.Abstractions;

namespace Concertable.B2B.Tenant.IntegrationTests;

/// <summary>
/// Invitations — create/list/revoke on <c>api/organization/invitations</c> and accept on
/// <c>api/invitation/{id}/accept</c>, through the real ASP.NET pipeline. Covers the invite guards
/// (duplicate, already-a-member), the invitation email capture, the accept flow (membership mint,
/// email-match gate, idempotency), the negative accept paths (expired, revoked, tenant-deleted),
/// and the <c>MembersInvite</c> permission boundary (Owner + Manager, not Staff).
/// </summary>
[Collection("Integration")]
public sealed class InvitationTests : IAsyncLifetime
{
    private readonly TenantApiFixture fixture;

    public InvitationTests(TenantApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    private Guid TenantOf(Guid userId) => fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == userId).Id;

    private Guid TenantFor(HttpClient client)
    {
        if (client.DefaultRequestHeaders.TryGetValues(TenantHeaders.TenantId, out var tenantIds))
            return Guid.Parse(tenantIds.Single());
        var userId = Guid.Parse(client.DefaultRequestHeaders.GetValues(TestAuthHandler.UserIdHeader).Single());
        return TenantOf(userId);
    }

    private Task<HttpResponseMessage> Invite(HttpClient client, string email, string presetKey) =>
        client.PostAsJsonAsync("/api/organization/invitations", new
        {
            email,
            roleIds = new[] { TenantApiFixture.RoleId(TenantFor(client), presetKey) },
        });

    private async Task<InvitationDto> InviteAsync(HttpClient client, string email, string presetKey)
    {
        var response = await Invite(client, email, presetKey);
        await response.ShouldBe(HttpStatusCode.Created);
        Assert.Equal("/api/organization/invitations", response.Headers.Location?.OriginalString);
        return (await response.Content.ReadAsync<InvitationDto>())!;
    }

    // A member who also owns another tenant must name the acting tenant explicitly, or resolution fails closed.
    private HttpClient ClientInTenant(Guid userId, string email, Guid tenantId)
    {
        var client = fixture.CreateClient(userId, email);
        client.DefaultRequestHeaders.Add(TenantHeaders.TenantId, tenantId.ToString());
        return client;
    }

    #region Invite

    [Fact]
    public async Task Invite_AsOwner_CreatesPendingInvitationAndSendsEmail()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        const string invitee = "newcomer@example.com";

        var dto = await InviteAsync(fixture.CreateClient(owner), invitee, "Manager");

        Assert.Equal(invitee, dto.Email);
        Assert.Contains(dto.Roles, role => role.Name == "Manager");

        var invitation = fixture.Invitations.Single(i => i.Id == dto.Id);
        var ownerMembership = fixture.Memberships.Single(
            membership => membership.TenantId == tenantId && membership.UserId == owner.Id);
        Assert.Equal(tenantId, invitation.TenantId);
        Assert.Equal(InvitationStatus.Pending, invitation.Status);
        Assert.Equal(ownerMembership.Id, invitation.InviterMembershipId);
        Assert.Equal(ownerMembership.PermissionVersion, invitation.InviterPermissionVersion);

        var email = Assert.Single(await fixture.GetStagedEmailsAsync(), e => e.To == invitee);
        Assert.Contains($"https://localhost:5177/settings/members/accept/{dto.Id}", email.Body);
    }

    [Fact]
    public async Task Invite_AsArtistOwner_SendsEmailWithBusinessPortalAcceptLink()
    {
        var owner = fixture.SeedState.ArtistManager1; // founding Owner of an artist tenant
        const string invitee = "artistcolleague@example.com";

        var dto = await InviteAsync(fixture.CreateClient(owner), invitee, "Manager");

        var email = Assert.Single(await fixture.GetStagedEmailsAsync(), e => e.To == invitee);
        Assert.Contains($"https://localhost:5177/settings/members/accept/{dto.Id}", email.Body);
    }

    [Fact]
    public async Task Invite_NormalizesEmail()
    {
        var owner = fixture.SeedState.VenueManager1;

        var dto = await InviteAsync(fixture.CreateClient(owner), "  MixedCase@Example.COM ", "Staff");

        Assert.Equal("mixedcase@example.com", dto.Email);
    }

    [Fact]
    public async Task Invite_DuplicatePending_IsConflict()
    {
        var owner = fixture.SeedState.VenueManager1;
        const string invitee = "dup@example.com";
        await InviteAsync(fixture.CreateClient(owner), invitee, "Manager");

        var second = await Invite(fixture.CreateClient(owner), invitee, "Staff");

        await second.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Invite_WhenPriorInviteExpired_Succeeds_AndRetiresTheExpiredOne()
    {
        // A lapsed invite stays Pending in storage (nothing sweeps it) — re-inviting must retire it, not 409.
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        const string invitee = "relapse@example.com";
        var expired = await fixture.AddInvitationAsync(tenantId, invitee, "Staff", owner.Id, DateTime.UtcNow.AddDays(-1));

        var dto = await InviteAsync(fixture.CreateClient(owner), invitee, "Manager");

        Assert.NotEqual(expired.Id, dto.Id);
        Assert.Equal(InvitationStatus.Expired, fixture.Invitations.Single(i => i.Id == expired.Id).Status);
        Assert.Equal(InvitationStatus.Pending, fixture.Invitations.Single(i => i.Id == dto.Id).Status);
    }

    [Fact]
    public async Task Invite_ExistingMemberEmail_IsConflict()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var member = fixture.SeedState.VenueManagerNoVenue;
        await fixture.AddMembershipAsync(tenantId, member.Id, "Staff");

        var response = await Invite(fixture.CreateClient(owner), member.Email, "Manager");

        await response.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Invite_AsManager_IsAllowed()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var manager = fixture.SeedState.VenueManagerNoVenue;
        await fixture.AddMembershipAsync(tenantId, manager.Id, "Manager");

        var response = await Invite(
            ClientInTenant(manager.Id, manager.Email, tenantId),
            "invitee@example.com",
            "Staff");

        await response.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Invite_AsManagerAssigningManager_IsForbidden()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var manager = fixture.SeedState.VenueManagerNoVenue;
        await fixture.AddMembershipAsync(tenantId, manager.Id, "Manager");

        var response = await Invite(
            ClientInTenant(manager.Id, manager.Email, tenantId),
            "invitee@example.com",
            "Manager");

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Invite_AsStaff_IsForbidden()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var staff = fixture.SeedState.VenueManagerNoVenue;
        await fixture.AddMembershipAsync(tenantId, staff.Id, "Staff");

        var response = await Invite(
            ClientInTenant(staff.Id, staff.Email, tenantId),
            "invitee@example.com",
            "Staff");

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GetInvitations

    [Fact]
    public async Task GetInvitations_ReturnsPending()
    {
        var owner = fixture.SeedState.VenueManager1;
        var client = fixture.CreateClient(owner);
        await InviteAsync(client, "pending@example.com", "Manager");

        var response = await client.GetAsync("/api/organization/invitations");

        await response.ShouldBe(HttpStatusCode.OK);
        var invitations = await response.Content.ReadAsync<List<InvitationDto>>();
        Assert.Contains(invitations!, i => i.Email == "pending@example.com");
    }

    [Fact]
    public async Task GetInvitations_ExcludesExpired()
    {
        // A lapsed invite stays Pending in storage, so the list must apply the expiry cut-off, not trust Status.
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var client = fixture.CreateClient(owner);
        await InviteAsync(client, "live@example.com", "Manager");
        var expired = await fixture.AddInvitationAsync(tenantId, "expired@example.com", "Staff", owner.Id, DateTime.UtcNow.AddDays(-1));

        var response = await client.GetAsync("/api/organization/invitations");

        await response.ShouldBe(HttpStatusCode.OK);
        var invitations = await response.Content.ReadAsync<List<InvitationDto>>();
        Assert.Contains(invitations!, i => i.Email == "live@example.com");
        Assert.DoesNotContain(invitations!, i => i.Id == expired.Id);
    }

    #endregion

    #region RevokeInvitation

    [Fact]
    public async Task Revoke_AsOwner_MarksRevoked()
    {
        var owner = fixture.SeedState.VenueManager1;
        var client = fixture.CreateClient(owner);
        var dto = await InviteAsync(client, "revoke@example.com", "Manager");

        var response = await client.DeleteAsync($"/api/organization/invitations/{dto.Id}");

        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(InvitationStatus.Revoked, fixture.Invitations.Single(i => i.Id == dto.Id).Status);
    }

    [Fact]
    public async Task Revoke_InvitationInAnotherTenant_IsNotFound()
    {
        var owner = fixture.SeedState.VenueManager1; // sole membership → own tenant resolves by default
        var otherTenantId = TenantOf(fixture.SeedState.ArtistManager1.Id);
        var foreign = await fixture.AddInvitationAsync(
            otherTenantId, "foreign@example.com", "Staff", fixture.SeedState.ArtistManager1.Id, DateTime.UtcNow.AddDays(7));

        var response = await fixture.CreateClient(owner).DeleteAsync($"/api/organization/invitations/{foreign.Id}");

        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Revoke_AcceptedInvitation_IsConflict_WithoutMutation()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var invitee = fixture.SeedState.VenueManagerNoVenue;
        var dto = await InviteAsync(fixture.CreateClient(owner), invitee.Email, "Manager");
        await (await fixture.CreateClient(invitee).PostAsync($"/api/invitation/{dto.Id}/accept")).ShouldBe(HttpStatusCode.OK);

        var response = await fixture.CreateClient(owner).DeleteAsync($"/api/organization/invitations/{dto.Id}");

        await response.ShouldBe(HttpStatusCode.Conflict);
        Assert.Equal(InvitationStatus.Accepted, fixture.Invitations.Single(i => i.Id == dto.Id).Status);
        Assert.Equal(1, fixture.Memberships.Count(m => m.TenantId == tenantId && m.UserId == invitee.Id));
    }

    #endregion

    #region Accept

    [Fact]
    public async Task Accept_ByExistingUser_CreatesMembership()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var invitee = fixture.SeedState.VenueManagerNoVenue;
        var dto = await InviteAsync(fixture.CreateClient(owner), invitee.Email, "Manager");

        var response = await fixture.CreateClient(invitee).PostAsync($"/api/invitation/{dto.Id}/accept");

        await response.ShouldBe(HttpStatusCode.OK);
        var joined = (await response.Content.ReadAsync<MembershipDto>())!;
        Assert.Equal(tenantId, joined.TenantId);
        Assert.Contains(joined.Roles, role => role.Name == "Manager");
        Assert.Equal([TenantBusinessActivityKind.VenueOperator], joined.BusinessActivities);
        var membership = fixture.Memberships.Single(m => m.TenantId == tenantId && m.UserId == invitee.Id);
        var ownerMembership = fixture.Memberships.Single(m => m.TenantId == tenantId && m.UserId == owner.Id);
        Assert.Equal(TenantApiFixture.RoleId(tenantId, "Manager"), Assert.Single(membership.Assignments).RoleId);
        Assert.Equal(ownerMembership.Id, membership.InvitedByMembershipId);
        Assert.Equal(InvitationStatus.Accepted, fixture.Invitations.Single(i => i.Id == dto.Id).Status);
    }

    [Fact]
    public async Task Accept_UnknownInvitation_IsNotFound_WithoutMutation()
    {
        var invitee = fixture.SeedState.VenueManagerNoVenue;
        var invitationCount = fixture.Invitations.Count();
        var membershipCount = fixture.Memberships.Count();

        var response = await fixture.CreateClient(invitee).PostAsync($"/api/invitation/{Guid.NewGuid()}/accept");

        await response.ShouldBe(HttpStatusCode.NotFound);
        Assert.Equal(invitationCount, fixture.Invitations.Count());
        Assert.Equal(membershipCount, fixture.Memberships.Count());
    }

    [Fact]
    public async Task Accept_SecondCall_IsConflict_WithoutDuplicateMembership()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var invitee = fixture.SeedState.VenueManagerNoVenue;
        var dto = await InviteAsync(fixture.CreateClient(owner), invitee.Email, "Manager");
        await (await fixture.CreateClient(invitee).PostAsync($"/api/invitation/{dto.Id}/accept")).ShouldBe(HttpStatusCode.OK);

        var second = await fixture.CreateClient(invitee).PostAsync($"/api/invitation/{dto.Id}/accept");

        await second.ShouldBe(HttpStatusCode.Conflict);
        Assert.Equal(1, fixture.Memberships.Count(m => m.TenantId == tenantId && m.UserId == invitee.Id));
    }

    [Fact]
    public async Task Accept_CallerEmailMismatch_IsForbidden()
    {
        var owner = fixture.SeedState.VenueManager1;
        var dto = await InviteAsync(fixture.CreateClient(owner), "someoneelse@example.com", "Manager");
        var wrongUser = fixture.SeedState.VenueManagerNoVenue; // a different email than the invitation

        var response = await fixture.CreateClient(wrongUser).PostAsync($"/api/invitation/{dto.Id}/accept");

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Accept_ExpiredInvitation_IsRejected_WithoutMembership()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var invitee = fixture.SeedState.VenueManagerNoVenue;
        var expired = await fixture.AddInvitationAsync(tenantId, invitee.Email, "Manager", owner.Id, DateTime.UtcNow.AddDays(-1));

        var response = await fixture.CreateClient(invitee).PostAsync($"/api/invitation/{expired.Id}/accept");

        await response.ShouldBe(HttpStatusCode.BadRequest);
        Assert.DoesNotContain(fixture.Memberships, m => m.TenantId == tenantId && m.UserId == invitee.Id);
    }

    [Fact]
    public async Task Accept_RevokedInvitation_IsRejected()
    {
        var owner = fixture.SeedState.VenueManager1;
        var ownerClient = fixture.CreateClient(owner);
        var invitee = fixture.SeedState.VenueManagerNoVenue;
        var dto = await InviteAsync(ownerClient, invitee.Email, "Manager");
        await (await ownerClient.DeleteAsync($"/api/organization/invitations/{dto.Id}")).ShouldBe(HttpStatusCode.NoContent);

        var response = await fixture.CreateClient(invitee).PostAsync($"/api/invitation/{dto.Id}/accept");

        await response.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Accept_InviterNoLongerAuthorizedForRole_IsForbidden()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var coOwner = fixture.SeedState.VenueManagerNoVenue;
        var invitee = fixture.SeedState.ArtistManagerNoArtist;
        await fixture.AddOwnerMembershipAsync(tenantId, coOwner.Id);
        var invitation = await InviteAsync(
            fixture.CreateClient(owner),
            invitee.Email,
            "Manager");
        var coOwnerClient = ClientInTenant(coOwner.Id, coOwner.Email, tenantId);
        await (await coOwnerClient.PutAsJsonAsync(
            $"/api/organization/members/{owner.Id}/roles",
            new { roleIds = new[] { TenantApiFixture.RoleId(tenantId, "Staff") } }))
            .ShouldBe(HttpStatusCode.NoContent);

        var response = await fixture.CreateClient(invitee)
            .PostAsync($"/api/invitation/{invitation.Id}/accept");

        await response.ShouldBe(HttpStatusCode.Forbidden);
        Assert.DoesNotContain(
            fixture.Memberships,
            membership => membership.TenantId == tenantId && membership.UserId == invitee.Id);
    }

    [Fact]
    public async Task Accept_InviterPermissionVersionChanged_IsForbidden()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var coOwner = fixture.SeedState.VenueManagerNoVenue;
        var invitee = fixture.SeedState.ArtistManagerNoArtist;
        await fixture.AddOwnerMembershipAsync(tenantId, coOwner.Id);
        var invitation = await InviteAsync(
            fixture.CreateClient(owner),
            invitee.Email,
            "Manager");
        var coOwnerClient = ClientInTenant(coOwner.Id, coOwner.Email, tenantId);
        await (await coOwnerClient.PutAsJsonAsync(
            $"/api/organization/members/{owner.Id}/roles",
            new { roleIds = new[] { TenantApiFixture.RoleId(tenantId, "Owner"), TenantApiFixture.RoleId(tenantId, "Manager") } }))
            .ShouldBe(HttpStatusCode.NoContent);

        var response = await fixture.CreateClient(invitee)
            .PostAsync($"/api/invitation/{invitation.Id}/accept");

        await response.ShouldBe(HttpStatusCode.Forbidden);
        Assert.DoesNotContain(
            fixture.Memberships,
            membership => membership.TenantId == tenantId && membership.UserId == invitee.Id);
    }

    [Fact]
    public async Task Accept_TenantNoLongerExists_IsRejected_WithoutMembership()
    {
        var owner = fixture.SeedState.VenueManagerNoVenue;
        var tenantId = TenantOf(owner.Id);
        var invitee = fixture.SeedState.ArtistManagerNoArtist;
        var ownerClient = fixture.CreateClient(owner);
        var inviteeClient = fixture.CreateClient(invitee);
        var invitation = await InviteAsync(ownerClient, invitee.Email, "Manager");

        var (deletion, response) = await fixture.RunWithPausedTenantDeletionAsync(
            invitation.Id,
            () => ownerClient.DeleteAsync("/api/organization"),
            () => inviteeClient.PostAsync($"/api/invitation/{invitation.Id}/accept"));

        await deletion.ShouldBe(HttpStatusCode.NoContent);
        await response.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadAsync<ProblemDetails>();
        Assert.Equal("tenant.accept_invitation_tenant_not_found", problem!.Extensions["code"]?.ToString());
        Assert.DoesNotContain(fixture.Memberships, m => m.TenantId == tenantId && m.UserId == invitee.Id);
    }

    #endregion

    #region DeleteActiveTenant

    [Fact]
    public async Task DeleteOrganization_RemovesTheTenantsInvitations()
    {
        var owner = fixture.SeedState.VenueManagerNoVenue;
        var tenantId = TenantOf(owner.Id);
        var client = fixture.CreateClient(owner);
        await InviteAsync(client, "cleanup@example.com", "Manager");

        var response = await client.DeleteAsync("/api/organization");

        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.DoesNotContain(fixture.Invitations, i => i.TenantId == tenantId);
    }

    #endregion
}
