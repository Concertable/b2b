using Concertable.Auth.Contracts;
using Concertable.Auth.Contracts.Events;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Concertable.B2B.Tenant.IntegrationTests;

[Collection("Integration")]
public sealed class TenantProvisioningTests : IAsyncLifetime
{
    private readonly TenantApiFixture fixture;

    public TenantProvisioningTests(TenantApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Theory]
    [InlineData(InteractiveClient.VenueBrowser, TenantBusinessActivityKind.VenueOperator)]
    [InlineData(InteractiveClient.ArtistBrowser, TenantBusinessActivityKind.Artist)]
    public async Task Registration_NewManager_ProvisionsTenantWithActivityAndFoundingOwner(
        InteractiveClient client,
        TenantBusinessActivityKind expected)
    {
        var userId = Guid.NewGuid();
        await fixture.ProvisionAsync(new CredentialRegisteredEvent(userId, $"{Guid.NewGuid():N}@test.com", InteractiveClientInfo.Get(client).Id));

        var membership = await fixture.Memberships.SingleOrDefaultAsync(m => m.UserId == userId);
        Assert.NotNull(membership);
        Assert.Equal(TenantRole.Owner, membership!.Role);
        Assert.Null(membership.InvitedByMembershipId);

        var tenant = await fixture.Tenants.SingleOrDefaultAsync(t => t.Id == membership.TenantId);
        Assert.NotNull(tenant);
        Assert.Equal(userId, tenant!.CreatedByUserId);

        var activity = Assert.Single(await fixture.BusinessActivities
            .Where(p => p.TenantId == membership.TenantId)
            .ToListAsync());
        Assert.Equal(expected, activity.Kind);
        Assert.Null(activity.RetiredAt);
    }

    [Theory]
    [InlineData(InteractiveClient.BusinessBrowser)]
    [InlineData(InteractiveClient.BusinessMobile)]
    public async Task Registration_BusinessClient_DoesNotInferTenantOrMarketplaceActivity(
        InteractiveClient client)
    {
        var userId = Guid.NewGuid();
        await fixture.ProvisionAsync(new CredentialRegisteredEvent(
            userId,
            $"{Guid.NewGuid():N}@test.com",
            InteractiveClientInfo.Get(client).Id));

        Assert.False(await fixture.Memberships.AnyAsync(candidate => candidate.UserId == userId));
        Assert.False(await fixture.Tenants.AnyAsync(candidate => candidate.CreatedByUserId == userId));
    }

    [Fact]
    public async Task Registration_NonManagerClient_ProvisionsNothing()
    {
        var userId = Guid.NewGuid();
        await fixture.ProvisionAsync(new CredentialRegisteredEvent(userId, "customer@test.com", InteractiveClientInfo.Get(InteractiveClient.CustomerBrowser).Id));

        Assert.False(await fixture.Memberships.AnyAsync(m => m.UserId == userId));
    }

    [Fact]
    public async Task Registration_InvitedEmail_JoinsInviterTenantAsMember_NoPersonalTenant()
    {
        var inviter = fixture.SeedState.VenueManager1;
        var tenantId = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == inviter.Id).Id;
        var newUserId = Guid.NewGuid();
        var newEmail = $"{Guid.NewGuid():N}@invited.test";
        await fixture.AddInvitationAsync(tenantId, newEmail, TenantRole.Manager, inviter.Id, DateTime.UtcNow.AddDays(7));

        await fixture.ProvisionAsync(new CredentialRegisteredEvent(newUserId, newEmail, InteractiveClientInfo.Get(InteractiveClient.VenueBrowser).Id));

        var membership = await fixture.Memberships.SingleOrDefaultAsync(m => m.UserId == newUserId);
        Assert.NotNull(membership);
        Assert.Equal(tenantId, membership!.TenantId);
        Assert.Equal(TenantRole.Manager, membership.Role);
        var inviterMembership = await fixture.Memberships.SingleAsync(
            candidate => candidate.TenantId == tenantId && candidate.UserId == inviter.Id);
        Assert.Equal(inviterMembership.Id, membership.InvitedByMembershipId);

        // The invited user joins the inviter's live tenant — no personal tenant, no re-Announce.
        Assert.False(await fixture.Tenants.AnyAsync(t => t.CreatedByUserId == newUserId));

        var invitation = await fixture.Invitations.SingleAsync(i => i.TenantId == tenantId && i.Email == newEmail);
        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        Assert.Equal(newUserId, invitation.AcceptedByUserId);
    }

    [Fact]
    public async Task Registration_InvitedEmail_MatchesCaseInsensitively()
    {
        var inviter = fixture.SeedState.VenueManager1;
        var tenantId = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == inviter.Id).Id;
        var newUserId = Guid.NewGuid();
        await fixture.AddInvitationAsync(tenantId, "invitee@casing.test", TenantRole.Staff, inviter.Id, DateTime.UtcNow.AddDays(7));

        // Auth carries the email verbatim; the handler normalizes it before matching the stored (normalized) invite.
        await fixture.ProvisionAsync(new CredentialRegisteredEvent(newUserId, "  Invitee@Casing.TEST ", InteractiveClientInfo.Get(InteractiveClient.VenueBrowser).Id));

        var membership = await fixture.Memberships.SingleOrDefaultAsync(m => m.UserId == newUserId);
        Assert.NotNull(membership);
        Assert.Equal(tenantId, membership!.TenantId);
        Assert.Equal(TenantRole.Staff, membership.Role);
    }

    [Fact]
    public async Task Registration_InvitedEmail_Redelivery_IsIdempotent()
    {
        var inviter = fixture.SeedState.VenueManager1;
        var tenantId = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == inviter.Id).Id;
        var newUserId = Guid.NewGuid();
        var newEmail = $"{Guid.NewGuid():N}@invited.test";
        await fixture.AddInvitationAsync(tenantId, newEmail, TenantRole.Manager, inviter.Id, DateTime.UtcNow.AddDays(7));

        // Same envelope → same MessageId → the inbox dedup swallows the redelivery.
        var envelope = MessageEnvelope.Create<CredentialRegisteredEvent>(DateTimeOffset.UtcNow);
        var e = new CredentialRegisteredEvent(newUserId, newEmail, InteractiveClientInfo.Get(InteractiveClient.VenueBrowser).Id);
        await fixture.ProvisionAsync(e, envelope);
        await fixture.ProvisionAsync(e, envelope);

        Assert.Equal(1, await fixture.Memberships.CountAsync(m => m.UserId == newUserId));
    }

    [Fact]
    public async Task Registration_InviterPermissionVersionChanged_ProvisionsPersonalTenant()
    {
        var inviter = fixture.SeedState.VenueManager1;
        var inviterTenantId = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == inviter.Id).Id;
        var newUserId = Guid.NewGuid();
        var newEmail = $"{Guid.NewGuid():N}@invited.test";
        var invitation = await fixture.AddInvitationAsync(
            inviterTenantId,
            newEmail,
            TenantRole.Manager,
            inviter.Id,
            DateTime.UtcNow.AddDays(7));
        await fixture.ChangeMembershipRoleAsync(inviterTenantId, inviter.Id, TenantRole.Owner);

        await fixture.ProvisionAsync(new CredentialRegisteredEvent(
            newUserId,
            newEmail,
            InteractiveClientInfo.Get(InteractiveClient.VenueBrowser).Id));

        var membership = await fixture.Memberships.SingleAsync(candidate => candidate.UserId == newUserId);
        Assert.Equal(TenantRole.Owner, membership.Role);
        Assert.NotEqual(inviterTenantId, membership.TenantId);
        Assert.Equal(
            InvitationStatus.Revoked,
            (await fixture.Invitations.SingleAsync(candidate => candidate.Id == invitation.Id)).Status);
    }

    [Fact]
    public async Task Registration_SeededOperator_IsIdempotent_SingleOwnerMembership()
    {
        var manager = fixture.SeedState.VenueManager1;

        /* The seeder already created this operator's tenant + Owner membership; re-running the handler (as the
           bus would on the real CredentialRegisteredEvent) must not duplicate either — the unique (TenantId,
           UserId) index would throw on a duplicate insert, so a clean run is itself the dedup assertion. */
        await fixture.ProvisionAsync(new CredentialRegisteredEvent(manager.Id, manager.Email, InteractiveClientInfo.Get(InteractiveClient.VenueBrowser).Id));

        var ownerCount = await fixture.Memberships.CountAsync(m => m.UserId == manager.Id && m.Role == TenantRole.Owner);
        var tenantCount = await fixture.Tenants.CountAsync(t => t.CreatedByUserId == manager.Id);

        Assert.Equal(1, ownerCount);
        Assert.Equal(1, tenantCount);
    }
}
