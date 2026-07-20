using System.Net;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.User.Domain.Entities;
using Xunit.Abstractions;

namespace Concertable.B2B.Tenant.IntegrationTests;

/// <summary>
/// Invitations — create/list/revoke on <c>api/organizations/invitations</c> and accept on
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

    private static Task<HttpResponseMessage> Invite(HttpClient client, string email, TenantRole role) =>
        client.PostAsync("/api/organizations/invitations", new { email, role = role.ToString() });

    private async Task<InvitationDto> InviteAsync(HttpClient client, string email, TenantRole role)
    {
        var response = await Invite(client, email, role);
        await response.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadAsync<InvitationDto>())!;
    }

    // A member who also owns another tenant must name the acting tenant explicitly, or resolution fails closed.
    private HttpClient ClientInTenant(UserEntity user, Guid tenantId)
    {
        var client = fixture.CreateClient(user);
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

        var dto = await InviteAsync(fixture.CreateClient(owner), invitee, TenantRole.Manager);

        Assert.Equal(invitee, dto.Email);
        Assert.Equal(TenantRole.Manager, dto.Role);

        var invitation = fixture.Invitations.Single(i => i.Id == dto.Id);
        Assert.Equal(tenantId, invitation.TenantId);
        Assert.Equal(InvitationStatus.Pending, invitation.Status);
        Assert.Equal(owner.Id, invitation.CreatedByUserId);

        var email = Assert.Single(fixture.EmailSender.Sent, e => e.To == invitee);
        Assert.Contains(dto.Id.ToString(), email.Body);
    }

    [Fact]
    public async Task Invite_NormalizesEmail()
    {
        var owner = fixture.SeedState.VenueManager1;

        var dto = await InviteAsync(fixture.CreateClient(owner), "  MixedCase@Example.COM ", TenantRole.Staff);

        Assert.Equal("mixedcase@example.com", dto.Email);
    }

    [Fact]
    public async Task Invite_DuplicatePending_IsConflict()
    {
        var owner = fixture.SeedState.VenueManager1;
        const string invitee = "dup@example.com";
        await InviteAsync(fixture.CreateClient(owner), invitee, TenantRole.Manager);

        var second = await Invite(fixture.CreateClient(owner), invitee, TenantRole.Staff);

        await second.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Invite_ExistingMemberEmail_IsConflict()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var member = fixture.SeedState.VenueManagerNoVenue;
        await fixture.AddMembershipAsync(tenantId, member.Id, TenantRole.Staff);

        var response = await Invite(fixture.CreateClient(owner), member.Email, TenantRole.Manager);

        await response.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Invite_AsManager_IsAllowed()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var manager = fixture.SeedState.VenueManagerNoVenue;
        await fixture.AddMembershipAsync(tenantId, manager.Id, TenantRole.Manager);

        var response = await Invite(ClientInTenant(manager, tenantId), "invitee@example.com", TenantRole.Staff);

        await response.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Invite_AsStaff_IsForbidden()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var staff = fixture.SeedState.VenueManagerNoVenue;
        await fixture.AddMembershipAsync(tenantId, staff.Id, TenantRole.Staff);

        var response = await Invite(ClientInTenant(staff, tenantId), "invitee@example.com", TenantRole.Staff);

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GetInvitations

    [Fact]
    public async Task GetInvitations_ReturnsPending()
    {
        var owner = fixture.SeedState.VenueManager1;
        var client = fixture.CreateClient(owner);
        await InviteAsync(client, "pending@example.com", TenantRole.Manager);

        var response = await client.GetAsync("/api/organizations/invitations");

        await response.ShouldBe(HttpStatusCode.OK);
        var invitations = await response.Content.ReadAsync<List<InvitationDto>>();
        Assert.Contains(invitations!, i => i.Email == "pending@example.com");
    }

    #endregion

    #region RevokeInvitation

    [Fact]
    public async Task Revoke_AsOwner_MarksRevoked()
    {
        var owner = fixture.SeedState.VenueManager1;
        var client = fixture.CreateClient(owner);
        var dto = await InviteAsync(client, "revoke@example.com", TenantRole.Manager);

        var response = await client.DeleteAsync($"/api/organizations/invitations/{dto.Id}");

        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(InvitationStatus.Revoked, fixture.Invitations.Single(i => i.Id == dto.Id).Status);
    }

    [Fact]
    public async Task Revoke_InvitationInAnotherTenant_IsNotFound()
    {
        var owner = fixture.SeedState.VenueManager1; // sole membership → own tenant resolves by default
        var otherTenantId = TenantOf(fixture.SeedState.ArtistManager1.Id);
        var foreign = await fixture.AddInvitationAsync(
            otherTenantId, "foreign@example.com", TenantRole.Staff, fixture.SeedState.ArtistManager1.Id, DateTime.UtcNow.AddDays(7));

        var response = await fixture.CreateClient(owner).DeleteAsync($"/api/organizations/invitations/{foreign.Id}");

        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Accept

    [Fact]
    public async Task Accept_ByExistingUser_CreatesMembership()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var invitee = fixture.SeedState.VenueManagerNoVenue;
        var dto = await InviteAsync(fixture.CreateClient(owner), invitee.Email, TenantRole.Manager);

        var response = await fixture.CreateClient(invitee).PostAsync($"/api/invitation/{dto.Id}/accept");

        await response.ShouldBe(HttpStatusCode.NoContent);
        var membership = fixture.Memberships.Single(m => m.TenantId == tenantId && m.UserId == invitee.Id);
        Assert.Equal(TenantRole.Manager, membership.Role);
        Assert.Equal(owner.Id, membership.InvitedByUserId);
        Assert.Equal(InvitationStatus.Accepted, fixture.Invitations.Single(i => i.Id == dto.Id).Status);
    }

    [Fact]
    public async Task Accept_SecondCall_IsConflict_WithoutDuplicateMembership()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var invitee = fixture.SeedState.VenueManagerNoVenue;
        var dto = await InviteAsync(fixture.CreateClient(owner), invitee.Email, TenantRole.Manager);
        await (await fixture.CreateClient(invitee).PostAsync($"/api/invitation/{dto.Id}/accept")).ShouldBe(HttpStatusCode.NoContent);

        var second = await fixture.CreateClient(invitee).PostAsync($"/api/invitation/{dto.Id}/accept");

        await second.ShouldBe(HttpStatusCode.Conflict);
        Assert.Equal(1, fixture.Memberships.Count(m => m.TenantId == tenantId && m.UserId == invitee.Id));
    }

    [Fact]
    public async Task Accept_CallerEmailMismatch_IsForbidden()
    {
        var owner = fixture.SeedState.VenueManager1;
        var dto = await InviteAsync(fixture.CreateClient(owner), "someoneelse@example.com", TenantRole.Manager);
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
        var expired = await fixture.AddInvitationAsync(tenantId, invitee.Email, TenantRole.Manager, owner.Id, DateTime.UtcNow.AddDays(-1));

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
        var dto = await InviteAsync(ownerClient, invitee.Email, TenantRole.Manager);
        await (await ownerClient.DeleteAsync($"/api/organizations/invitations/{dto.Id}")).ShouldBe(HttpStatusCode.NoContent);

        var response = await fixture.CreateClient(invitee).PostAsync($"/api/invitation/{dto.Id}/accept");

        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Accept_TenantNoLongerExists_IsRejected_WithoutMembership()
    {
        var invitee = fixture.SeedState.VenueManagerNoVenue;
        var ghostTenantId = Guid.NewGuid();
        var orphan = await fixture.AddInvitationAsync(
            ghostTenantId, invitee.Email, TenantRole.Manager, fixture.SeedState.VenueManager1.Id, DateTime.UtcNow.AddDays(7));

        var response = await fixture.CreateClient(invitee).PostAsync($"/api/invitation/{orphan.Id}/accept");

        await response.ShouldBe(HttpStatusCode.NotFound);
        Assert.DoesNotContain(fixture.Memberships, m => m.TenantId == ghostTenantId && m.UserId == invitee.Id);
    }

    #endregion

    #region DeleteCurrentTenant

    [Fact]
    public async Task DeleteOrganization_RemovesTheTenantsInvitations()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var client = fixture.CreateClient(owner);
        await InviteAsync(client, "cleanup@example.com", TenantRole.Manager);

        var response = await client.DeleteAsync("/api/organizations");

        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.DoesNotContain(fixture.Invitations, i => i.TenantId == tenantId);
    }

    #endregion
}
