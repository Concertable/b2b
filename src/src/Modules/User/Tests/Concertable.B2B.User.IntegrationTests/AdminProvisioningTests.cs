using Concertable.Auth.Contracts;
using Concertable.Auth.Contracts.Events;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.User.Contracts;
using Concertable.B2B.User.Domain.Entities;
using Concertable.B2B.User.Infrastructure.Events;
using Concertable.Messaging.Contracts;
using Concertable.Seed.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Concertable.B2B.User.IntegrationTests;

[Collection("Integration")]
public sealed class AdminProvisioningTests : IAsyncLifetime
{
    private readonly UserApiFixture fixture;

    public AdminProvisioningTests(UserApiFixture fixture, ITestOutputHelper output)
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
            .OfType<CredentialRegisteredHandler>()
            .Single();
        await handler.HandleAsync(e, envelope ?? MessageEnvelope.Create<CredentialRegisteredEvent>(DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task Registration_MatchingPendingInvitation_GrantsAdminProfile()
    {
        var inviter = fixture.SeedState.Admin;
        var newUserId = Guid.NewGuid();
        var newEmail = $"{Guid.NewGuid():N}@invited.test";
        var invitation = await fixture.AddAdminInvitationAsync(newEmail, inviter.Id, DateTime.UtcNow.AddDays(7));

        await ProvisionAsync(new CredentialRegisteredEvent(newUserId, newEmail, ClientIds.Admin));

        Assert.True(await fixture.IsAdminAsync(newUserId));
        var accepted = await fixture.AdminInvitations.SingleAsync(i => i.Id == invitation.Id);
        Assert.Equal(AdminInvitationStatus.Accepted, accepted.Status);
        Assert.Equal(newUserId, accepted.AcceptedByUserId);
    }

    [Fact]
    public async Task Registration_InvitedEmail_MatchesCaseInsensitively()
    {
        var inviter = fixture.SeedState.Admin;
        var newUserId = Guid.NewGuid();
        await fixture.AddAdminInvitationAsync("invitee@casing.test", inviter.Id, DateTime.UtcNow.AddDays(7));

        // Auth carries the email verbatim; the handler normalizes it before matching the stored (normalized) invite.
        await ProvisionAsync(new CredentialRegisteredEvent(newUserId, "  Invitee@Casing.TEST ", ClientIds.Admin));

        Assert.True(await fixture.IsAdminAsync(newUserId));
    }

    [Fact]
    public async Task Registration_ExpiredInvitation_GrantsNoAdminProfile()
    {
        var inviter = fixture.SeedState.Admin;
        var newUserId = Guid.NewGuid();
        var newEmail = $"{Guid.NewGuid():N}@invited.test";
        await fixture.AddAdminInvitationAsync(newEmail, inviter.Id, DateTime.UtcNow.AddDays(-1));

        await ProvisionAsync(new CredentialRegisteredEvent(newUserId, newEmail, ClientIds.Admin));

        Assert.False(await fixture.IsAdminAsync(newUserId));
    }

    [Fact]
    public async Task Registration_BootstrapEmail_GrantsAdminProfile_WhenNoAdminExistsYet()
    {
        await fixture.ClearAdminsAsync();
        var newUserId = Guid.NewGuid();

        await ProvisionAsync(new CredentialRegisteredEvent(newUserId, SeedUsers.AdminEmail, ClientIds.Admin));

        Assert.True(await fixture.IsAdminAsync(newUserId));
    }

    [Fact]
    public async Task Registration_BootstrapEmail_GrantsNoAdminProfile_WhenAnAdminAlreadyExists()
    {
        // The standard seed graph's admin already occupies SeedUsers.AdminEmail (the dev-default bootstrap
        // email), which real registration can never collide with (Auth enforces global email uniqueness) —
        // so free it up and provision a distinct admin first, proving it's the AdminProfiles-non-empty gate,
        // not an artificial email collision, that keeps bootstrap closed.
        await fixture.ClearAdminsAsync();
        var existingAdminUserId = Guid.NewGuid();
        var existingAdminEmail = $"{Guid.NewGuid():N}@existing-admin.test";
        await fixture.AddAdminInvitationAsync(existingAdminEmail, Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
        await ProvisionAsync(new CredentialRegisteredEvent(existingAdminUserId, existingAdminEmail, ClientIds.Admin));
        Assert.True(await fixture.IsAdminAsync(existingAdminUserId));

        var newUserId = Guid.NewGuid();
        await ProvisionAsync(new CredentialRegisteredEvent(newUserId, SeedUsers.AdminEmail, ClientIds.Admin));

        Assert.False(await fixture.IsAdminAsync(newUserId));
    }

    [Fact]
    public async Task Registration_NoInvitationAndNonBootstrapEmail_CreatesUserButGrantsNoAdminProfile()
    {
        var newUserId = Guid.NewGuid();
        var newEmail = $"{Guid.NewGuid():N}@uninvited.test";

        await ProvisionAsync(new CredentialRegisteredEvent(newUserId, newEmail, ClientIds.Admin));

        using var scope = fixture.Services.CreateScope();
        var userModule = scope.ServiceProvider.GetRequiredService<IUserModule>();
        Assert.True((await userModule.GetByIdAsync(newUserId)).TryGetValue(out _));
        Assert.False(await fixture.IsAdminAsync(newUserId));
    }

    [Fact]
    public async Task Registration_NonAdminClient_ProvisionsNothing()
    {
        var newUserId = Guid.NewGuid();

        await ProvisionAsync(new CredentialRegisteredEvent(newUserId, "venue-manager@test.com", ClientIds.VenueWeb));

        Assert.False(await fixture.IsAdminAsync(newUserId));
    }

    [Fact]
    public async Task Registration_MatchingPendingInvitation_Redelivery_IsIdempotent()
    {
        var inviter = fixture.SeedState.Admin;
        var newUserId = Guid.NewGuid();
        var newEmail = $"{Guid.NewGuid():N}@invited.test";
        await fixture.AddAdminInvitationAsync(newEmail, inviter.Id, DateTime.UtcNow.AddDays(7));

        // Same envelope → same MessageId → the inbox dedup swallows the redelivery.
        var envelope = MessageEnvelope.Create<CredentialRegisteredEvent>(DateTimeOffset.UtcNow);
        var e = new CredentialRegisteredEvent(newUserId, newEmail, ClientIds.Admin);
        await ProvisionAsync(e, envelope);
        await ProvisionAsync(e, envelope);

        Assert.True(await fixture.IsAdminAsync(newUserId));
    }
}
