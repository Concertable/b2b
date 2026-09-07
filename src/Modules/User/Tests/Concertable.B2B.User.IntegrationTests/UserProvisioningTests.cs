using Concertable.Auth.Contracts;
using Concertable.Auth.Contracts.Events;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
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

    [Theory]
    [InlineData(ClientIds.VenueWeb)]
    [InlineData(ClientIds.ArtistWeb)]
    [InlineData(ClientIds.Admin)]
    public async Task Registration_ManagerClient_CreatesUser(string clientId)
    {
        var userId = Guid.NewGuid();
        var email = $"{Guid.NewGuid():N}@test.com";

        await fixture.ProvisionAsync(new CredentialRegisteredEvent(userId, email, clientId));

        var user = await fixture.Users.SingleOrDefaultAsync(value => value.Id == userId);
        Assert.NotNull(user);
        Assert.Equal(email, user!.Email);
    }

    [Fact]
    public async Task Registration_NonManagerClient_CreatesNothing()
    {
        var userId = Guid.NewGuid();

        await fixture.ProvisionAsync(new CredentialRegisteredEvent(userId, "customer@test.com", ClientIds.CustomerWeb));

        Assert.False(await fixture.Users.AnyAsync(value => value.Id == userId));
    }

    [Fact]
    public async Task Registration_Redelivery_IsIdempotent()
    {
        var userId = Guid.NewGuid();
        var email = $"{Guid.NewGuid():N}@test.com";
        var envelope = MessageEnvelope.Create<CredentialRegisteredEvent>(DateTimeOffset.UtcNow);
        var @event = new CredentialRegisteredEvent(userId, email, ClientIds.VenueWeb);

        await fixture.ProvisionAsync(@event, envelope);
        await fixture.ProvisionAsync(@event, envelope);

        Assert.Equal(1, await fixture.Users.CountAsync(value => value.Id == userId));
    }
}
