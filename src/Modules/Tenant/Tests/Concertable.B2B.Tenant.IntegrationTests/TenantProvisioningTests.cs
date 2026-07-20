using Concertable.Auth.Contracts;
using Concertable.Auth.Contracts.Events;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Infrastructure.Events;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Concertable.B2B.Tenant.IntegrationTests;

/// <summary>
/// Registration provisions a tenant + its founding Owner membership and fixes the persona from the
/// registration client-id. Drives the production trigger directly — the <see cref="TenantProvisioningHandler"/>
/// reacting to a <see cref="CredentialRegisteredEvent"/> — since integration tests have no Auth service to
/// publish it. The seeded operators exercise the idempotent existing-tenant branch.
/// </summary>
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

    private async Task ProvisionAsync(CredentialRegisteredEvent e, MessageEnvelope? envelope = null)
    {
        using var scope = fixture.Services.CreateScope();
        var handler = scope.ServiceProvider
            .GetServices<IIntegrationEventHandler<CredentialRegisteredEvent>>()
            .OfType<TenantProvisioningHandler>()
            .Single();
        await handler.HandleAsync(e, envelope ?? MessageEnvelope.Create<CredentialRegisteredEvent>(DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(ClientIds.VenueWeb, TenantType.Venue)]
    [InlineData(ClientIds.ArtistWeb, TenantType.Artist)]
    public async Task Registration_NewManager_ProvisionsTenantWithPersonaAndFoundingOwner(string clientId, TenantType expected)
    {
        var userId = Guid.NewGuid();
        await ProvisionAsync(new CredentialRegisteredEvent(userId, $"{Guid.NewGuid():N}@test.com", clientId));

        var membership = await fixture.Memberships.SingleOrDefaultAsync(m => m.UserId == userId);
        Assert.NotNull(membership);
        Assert.Equal(TenantRole.Owner, membership!.Role);
        Assert.Null(membership.InvitedByUserId);

        var tenant = await fixture.Tenants.SingleOrDefaultAsync(t => t.Id == membership.TenantId);
        Assert.NotNull(tenant);
        Assert.Equal(expected, tenant!.Type);
        Assert.Equal(userId, tenant.CreatedByUserId);
    }

    [Fact]
    public async Task Registration_NonManagerClient_ProvisionsNothing()
    {
        var userId = Guid.NewGuid();
        await ProvisionAsync(new CredentialRegisteredEvent(userId, "customer@test.com", ClientIds.CustomerWeb));

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

        await ProvisionAsync(new CredentialRegisteredEvent(newUserId, newEmail, ClientIds.VenueWeb));

        var membership = await fixture.Memberships.SingleOrDefaultAsync(m => m.UserId == newUserId);
        Assert.NotNull(membership);
        Assert.Equal(tenantId, membership!.TenantId);
        Assert.Equal(TenantRole.Manager, membership.Role);
        Assert.Equal(inviter.Id, membership.InvitedByUserId);

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
        await ProvisionAsync(new CredentialRegisteredEvent(newUserId, "  Invitee@Casing.TEST ", ClientIds.VenueWeb));

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
        var e = new CredentialRegisteredEvent(newUserId, newEmail, ClientIds.VenueWeb);
        await ProvisionAsync(e, envelope);
        await ProvisionAsync(e, envelope);

        Assert.Equal(1, await fixture.Memberships.CountAsync(m => m.UserId == newUserId));
    }

    [Fact]
    public async Task Registration_SeededOperator_IsIdempotent_SingleOwnerMembership()
    {
        var manager = fixture.SeedState.VenueManager1;

        /* The seeder already created this operator's tenant + Owner membership; re-running the handler (as the
           bus would on the real CredentialRegisteredEvent) must not duplicate either — the unique (TenantId,
           UserId) index would throw on a duplicate insert, so a clean run is itself the dedup assertion. */
        await ProvisionAsync(new CredentialRegisteredEvent(manager.Id, manager.Email, ClientIds.VenueWeb));

        var ownerCount = await fixture.Memberships.CountAsync(m => m.UserId == manager.Id && m.Role == TenantRole.Owner);
        var tenantCount = await fixture.Tenants.CountAsync(t => t.CreatedByUserId == manager.Id);

        Assert.Equal(1, ownerCount);
        Assert.Equal(1, tenantCount);
    }
}
