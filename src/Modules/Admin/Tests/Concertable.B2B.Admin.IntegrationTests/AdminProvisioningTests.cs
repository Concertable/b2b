using Concertable.Auth.Contracts;
using Concertable.Auth.Contracts.Events;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.User.Domain.Entities;
using Concertable.Messaging.Contracts;
using Concertable.Seed.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

    // Creates the plain UserEntity precondition a login needs — UserEntity creation and inbox dedup are
    // asserted in Concertable.B2B.User.IntegrationTests' UserProvisioningTests, not here.
    private async Task RegisterAsync(CredentialRegisteredEvent e, MessageEnvelope? envelope = null)
    {
        using var scope = fixture.Services.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<CredentialRegisteredEvent>>();
        foreach (var handler in handlers)
            await handler.HandleAsync(e, envelope ?? MessageEnvelope.Create<CredentialRegisteredEvent>(DateTimeOffset.UtcNow));
    }

    /// <summary>Simulates the first authenticated request after login (what every SPA calls right after
    /// signin) — the point where <c>AdminService.EnsureCurrentUserAdminGrantedIfEligibleAsync</c> actually
    /// runs. Registration alone (<see cref="RegisterAsync"/>) only ever creates the plain <see cref="UserEntity"/>.</summary>
    private Task LogInAsync(Guid userId, string email) =>
        fixture.CreateClient(UserEntity.FromRegistration(userId, email)).GetAsync("/api/auth/me");

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
        var accepted = await fixture.AdminInvitations.SingleAsync(i => i.Id == invitation.Id);
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

        // Auth carries the email verbatim; the grant normalizes it before matching the stored (normalized) invite.
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
        var newUserId = Guid.NewGuid();
        await RegisterAsync(new CredentialRegisteredEvent(newUserId, SeedUsers.AdminEmail, ClientIds.Admin));

        await LogInAsync(newUserId, SeedUsers.AdminEmail);

        Assert.True(await fixture.IsAdminAsync(newUserId));
    }

    [Fact]
    public async Task Login_BootstrapEmail_GrantsNoAdminProfile_WhenAnAdminAlreadyExists()
    {
        // The standard seed graph's admin already occupies SeedUsers.AdminEmail (the dev-default bootstrap
        // email), which real registration can never collide with (Auth enforces global email uniqueness) —
        // so free it up and provision a distinct admin first, proving it's the AdminProfiles-non-empty gate,
        // not an artificial email collision, that keeps bootstrap closed.
        await fixture.ClearAdminsAsync();
        var existingAdminUserId = Guid.NewGuid();
        var existingAdminEmail = $"{Guid.NewGuid():N}@existing-admin.test";
        await fixture.AddAdminInvitationAsync(existingAdminEmail, Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
        await RegisterAsync(new CredentialRegisteredEvent(existingAdminUserId, existingAdminEmail, ClientIds.Admin));
        await LogInAsync(existingAdminUserId, existingAdminEmail);
        Assert.True(await fixture.IsAdminAsync(existingAdminUserId));

        var newUserId = Guid.NewGuid();
        await RegisterAsync(new CredentialRegisteredEvent(newUserId, SeedUsers.AdminEmail, ClientIds.Admin));
        await LogInAsync(newUserId, SeedUsers.AdminEmail);

        Assert.False(await fixture.IsAdminAsync(newUserId));
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
