using Concertable.Auth.Contracts;
using Concertable.Auth.Contracts.Events;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.User.Infrastructure.Data;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Concertable.B2B.User.IntegrationTests;

[Collection("Integration")]
public sealed class UserProvisioningTests : IAsyncLifetime
{
    private readonly UserApiFixture fixture;

    public UserProvisioningTests(UserApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    private async Task ProvisionAsync(CredentialRegisteredEvent e, MessageEnvelope? envelope = null)
    {
        using var scope = fixture.Services.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<CredentialRegisteredEvent>>();
        foreach (var handler in handlers)
            await handler.HandleAsync(e, envelope ?? MessageEnvelope.Create<CredentialRegisteredEvent>(DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(ClientIds.VenueWeb)]
    [InlineData(ClientIds.ArtistWeb)]
    [InlineData(ClientIds.Admin)]
    public async Task Registration_ManagerClient_CreatesUser(string clientId)
    {
        var userId = Guid.NewGuid();
        var email = $"{Guid.NewGuid():N}@test.com";

        await ProvisionAsync(new CredentialRegisteredEvent(userId, email, clientId));

        using var scope = fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        var user = await context.Users.SingleOrDefaultAsync(u => u.Id == userId);
        Assert.NotNull(user);
        Assert.Equal(email, user!.Email);
    }

    [Fact]
    public async Task Registration_NonManagerClient_CreatesNothing()
    {
        var userId = Guid.NewGuid();

        await ProvisionAsync(new CredentialRegisteredEvent(userId, "customer@test.com", ClientIds.CustomerWeb));

        using var scope = fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        Assert.False(await context.Users.AnyAsync(u => u.Id == userId));
    }

    [Fact]
    public async Task Registration_Redelivery_IsIdempotent()
    {
        var userId = Guid.NewGuid();
        var email = $"{Guid.NewGuid():N}@test.com";

        // Same envelope → same MessageId → the inbox dedup swallows the redelivery.
        var envelope = MessageEnvelope.Create<CredentialRegisteredEvent>(DateTimeOffset.UtcNow);
        var e = new CredentialRegisteredEvent(userId, email, ClientIds.VenueWeb);
        await ProvisionAsync(e, envelope);
        await ProvisionAsync(e, envelope);

        using var scope = fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        Assert.Equal(1, await context.Users.CountAsync(u => u.Id == userId));
    }

    [Fact]
    public async Task Registration_AdminClient_CreatesUser_EvenWithNoMatchingAdminGrant()
    {
        // User creation and the cross-module admin-grant call share one ambient transaction
        // (CredentialRegisteredHandler -> IAdminModule.GrantIfEligibleAsync). A registration with no
        // matching invitation and a non-bootstrap email grants no admin, but must still create the user.
        var userId = Guid.NewGuid();
        var email = $"{Guid.NewGuid():N}@uninvited.test";

        await ProvisionAsync(new CredentialRegisteredEvent(userId, email, ClientIds.Admin));

        using var scope = fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        var user = await context.Users.SingleOrDefaultAsync(u => u.Id == userId);
        Assert.NotNull(user);
    }
}
