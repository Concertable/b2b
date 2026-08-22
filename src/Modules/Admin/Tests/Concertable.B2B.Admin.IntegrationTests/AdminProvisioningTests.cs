using Concertable.Auth.Contracts;
using Concertable.Auth.Contracts.Events;
using Concertable.B2B.Admin.Domain.Entities;
using Concertable.Messaging.Contracts;
using Concertable.Seed.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Concertable.B2B.Admin.IntegrationTests;

[Collection("Integration")]
public sealed class AdminProvisioningTests : IAsyncLifetime
{
    private readonly AdminApiFixture fixture;

    public AdminProvisioningTests(AdminApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    private Task RegisterAsync(CredentialRegisteredEvent @event, MessageEnvelope? envelope = null) =>
        fixture.DispatchIntegrationEventAsync(
            @event,
            envelope ?? MessageEnvelope.Create<CredentialRegisteredEvent>(DateTimeOffset.UtcNow));

    private async Task LogInAsync(Guid userId, string email)
    {
        var response = await fixture.CreateClient(userId, email).GetAsync("/api/auth/me");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Login_MatchingPendingInvitation_GrantsAdminProfile()
    {
        var inviter = fixture.SeedState.Admin;
        var newUserId = Guid.NewGuid();
        var newEmail = $"{Guid.NewGuid():N}@invited.test";
        var invitation = await fixture.AddAdminInvitationAsync(newEmail, inviter.Id, DateTime.UtcNow.AddDays(7));
        await RegisterAsync(new CredentialRegisteredEvent(newUserId, newEmail, ClientIds.Admin));

        await LogInAsync(newUserId, newEmail);

        Assert.True(await fixture.IsAdminAsync(newUserId));
        var accepted = await fixture.AdminInvitations.SingleAsync(value => value.Id == invitation.Id);
        Assert.Equal(AdminInvitationStatus.Accepted, accepted.Status);
        Assert.Equal(newUserId, accepted.AcceptedByUserId);
    }

    [Fact]
    public async Task Registration_MatchingPendingInvitation_GrantsNoAdminProfileYet()
    {
        var inviter = fixture.SeedState.Admin;
        var newUserId = Guid.NewGuid();
        var newEmail = $"{Guid.NewGuid():N}@invited.test";
        await fixture.AddAdminInvitationAsync(newEmail, inviter.Id, DateTime.UtcNow.AddDays(7));

        await RegisterAsync(new CredentialRegisteredEvent(newUserId, newEmail, ClientIds.Admin));

        Assert.False(await fixture.IsAdminAsync(newUserId));
    }

    [Fact]
    public async Task Login_InvitedEmail_MatchesCaseInsensitively()
    {
        var inviter = fixture.SeedState.Admin;
        var newUserId = Guid.NewGuid();
        await fixture.AddAdminInvitationAsync("invitee@casing.test", inviter.Id, DateTime.UtcNow.AddDays(7));
        var rawEmail = "  Invitee@Casing.TEST ";
        await RegisterAsync(new CredentialRegisteredEvent(newUserId, rawEmail, ClientIds.Admin));

        await LogInAsync(newUserId, rawEmail);

        Assert.True(await fixture.IsAdminAsync(newUserId));
    }

    [Fact]
    public async Task Login_ExpiredInvitation_GrantsNoAdminProfile()
    {
        var inviter = fixture.SeedState.Admin;
        var newUserId = Guid.NewGuid();
        var newEmail = $"{Guid.NewGuid():N}@invited.test";
        await fixture.AddAdminInvitationAsync(newEmail, inviter.Id, DateTime.UtcNow.AddDays(-1));
        await RegisterAsync(new CredentialRegisteredEvent(newUserId, newEmail, ClientIds.Admin));

        await LogInAsync(newUserId, newEmail);

        Assert.False(await fixture.IsAdminAsync(newUserId));
    }

    [Fact]
    public async Task Login_BootstrapEmail_GrantsAdminProfile_WhenNoAdminExistsYet()
    {
        await fixture.ClearAdminsAsync();
        var userId = fixture.SeedState.Admin.Id;

        await LogInAsync(userId, SeedUsers.AdminEmail);

        Assert.True(await fixture.IsAdminAsync(userId));
    }

    [Fact]
    public async Task Login_BootstrapEmail_GrantsNoAdminProfile_WhenAnAdminAlreadyExists()
    {
        await fixture.ClearAdminsAsync();
        var existingAdminUserId = Guid.NewGuid();
        var existingAdminEmail = $"{Guid.NewGuid():N}@existing-admin.test";
        await fixture.AddAdminInvitationAsync(existingAdminEmail, Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
        await RegisterAsync(new CredentialRegisteredEvent(existingAdminUserId, existingAdminEmail, ClientIds.Admin));
        await LogInAsync(existingAdminUserId, existingAdminEmail);
        Assert.True(await fixture.IsAdminAsync(existingAdminUserId));

        var bootstrapUserId = fixture.SeedState.Admin.Id;
        await LogInAsync(bootstrapUserId, SeedUsers.AdminEmail);

        Assert.False(await fixture.IsAdminAsync(bootstrapUserId));
    }

    [Fact]
    public async Task Login_NoInvitationAndNonBootstrapEmail_GrantsNoAdminProfile()
    {
        var newUserId = Guid.NewGuid();
        var newEmail = $"{Guid.NewGuid():N}@uninvited.test";
        await RegisterAsync(new CredentialRegisteredEvent(newUserId, newEmail, ClientIds.Admin));

        await LogInAsync(newUserId, newEmail);

        Assert.False(await fixture.IsAdminAsync(newUserId));
    }

    [Fact]
    public async Task Login_AlreadyAdmin_DoesNotReAcceptInvitationOrDuplicateGrant()
    {
        var inviter = fixture.SeedState.Admin;
        var newUserId = Guid.NewGuid();
        var newEmail = $"{Guid.NewGuid():N}@invited.test";
        await fixture.AddAdminInvitationAsync(newEmail, inviter.Id, DateTime.UtcNow.AddDays(7));
        await RegisterAsync(new CredentialRegisteredEvent(newUserId, newEmail, ClientIds.Admin));
        await LogInAsync(newUserId, newEmail);
        Assert.True(await fixture.IsAdminAsync(newUserId));

        await LogInAsync(newUserId, newEmail);

        Assert.True(await fixture.IsAdminAsync(newUserId));
    }
}
