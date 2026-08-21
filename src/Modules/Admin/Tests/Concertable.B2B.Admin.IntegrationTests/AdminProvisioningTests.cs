using Concertable.B2B.Admin.Domain.Entities;
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

    [Fact]
    public async Task GrantIfEligibleAsync_MatchingPendingInvitation_GrantsAdminProfile()
    {
        var inviter = fixture.SeedState.Admin;
        var newUserId = Guid.NewGuid();
        var newEmail = $"{Guid.NewGuid():N}@invited.test";
        var invitation = await fixture.AddAdminInvitationAsync(newEmail, inviter.Id, DateTime.UtcNow.AddDays(7));

        await fixture.GrantIfEligibleAsync(newUserId, newEmail);

        Assert.True(await fixture.IsAdminAsync(newUserId));
        var accepted = await fixture.AdminInvitations.SingleAsync(i => i.Id == invitation.Id);
        Assert.Equal(AdminInvitationStatus.Accepted, accepted.Status);
        Assert.Equal(newUserId, accepted.AcceptedByUserId);
    }

    [Fact]
    public async Task GrantIfEligibleAsync_InvitedEmail_MatchesCaseInsensitively()
    {
        var inviter = fixture.SeedState.Admin;
        var newUserId = Guid.NewGuid();
        await fixture.AddAdminInvitationAsync("invitee@casing.test", inviter.Id, DateTime.UtcNow.AddDays(7));

        // Auth carries the email verbatim; the service normalizes it before matching the stored (normalized) invite.
        await fixture.GrantIfEligibleAsync(newUserId, "  Invitee@Casing.TEST ");

        Assert.True(await fixture.IsAdminAsync(newUserId));
    }

    [Fact]
    public async Task GrantIfEligibleAsync_ExpiredInvitation_GrantsNoAdminProfile()
    {
        var inviter = fixture.SeedState.Admin;
        var newUserId = Guid.NewGuid();
        var newEmail = $"{Guid.NewGuid():N}@invited.test";
        await fixture.AddAdminInvitationAsync(newEmail, inviter.Id, DateTime.UtcNow.AddDays(-1));

        await fixture.GrantIfEligibleAsync(newUserId, newEmail);

        Assert.False(await fixture.IsAdminAsync(newUserId));
    }

    [Fact]
    public async Task GrantIfEligibleAsync_BootstrapEmail_GrantsAdminProfile_WhenNoAdminExistsYet()
    {
        await fixture.ClearAdminsAsync();
        var newUserId = Guid.NewGuid();

        await fixture.GrantIfEligibleAsync(newUserId, SeedUsers.AdminEmail);

        Assert.True(await fixture.IsAdminAsync(newUserId));
    }

    [Fact]
    public async Task GrantIfEligibleAsync_BootstrapEmail_GrantsNoAdminProfile_WhenAnAdminAlreadyExists()
    {
        // The standard seed graph's admin already occupies SeedUsers.AdminEmail (the dev-default bootstrap
        // email), which real registration can never collide with (Auth enforces global email uniqueness) —
        // so free it up and provision a distinct admin first, proving it's the AdminProfiles-non-empty gate,
        // not an artificial email collision, that keeps bootstrap closed.
        await fixture.ClearAdminsAsync();
        var existingAdminUserId = Guid.NewGuid();
        var existingAdminEmail = $"{Guid.NewGuid():N}@existing-admin.test";
        await fixture.AddAdminInvitationAsync(existingAdminEmail, Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
        await fixture.GrantIfEligibleAsync(existingAdminUserId, existingAdminEmail);
        Assert.True(await fixture.IsAdminAsync(existingAdminUserId));

        var newUserId = Guid.NewGuid();
        await fixture.GrantIfEligibleAsync(newUserId, SeedUsers.AdminEmail);

        Assert.False(await fixture.IsAdminAsync(newUserId));
    }

    [Fact]
    public async Task GrantIfEligibleAsync_NoInvitationAndNonBootstrapEmail_GrantsNoAdminProfile()
    {
        var newUserId = Guid.NewGuid();
        var newEmail = $"{Guid.NewGuid():N}@uninvited.test";

        await fixture.GrantIfEligibleAsync(newUserId, newEmail);

        Assert.False(await fixture.IsAdminAsync(newUserId));
    }

    [Fact]
    public async Task GrantIfEligibleAsync_CalledTwiceForSameInvitation_IsIdempotent()
    {
        var inviter = fixture.SeedState.Admin;
        var newUserId = Guid.NewGuid();
        var newEmail = $"{Guid.NewGuid():N}@invited.test";
        await fixture.AddAdminInvitationAsync(newEmail, inviter.Id, DateTime.UtcNow.AddDays(7));

        // A redelivered registration event calls this twice with identical inputs; the invitation is no
        // longer Pending after the first grant, so the second call is naturally a no-op.
        await fixture.GrantIfEligibleAsync(newUserId, newEmail);
        await fixture.GrantIfEligibleAsync(newUserId, newEmail);

        Assert.True(await fixture.IsAdminAsync(newUserId));
    }
}
