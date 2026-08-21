using System.Net;
using System.Text.Json;
using Concertable.Auth.Contracts;
using Concertable.Auth.Contracts.Events;
using Concertable.Messaging.Contracts;
using Xunit.Abstractions;

namespace Concertable.B2B.Process.IntegrationTests;

[Collection("Integration")]
public sealed class RegistrationJourneyTests : IAsyncLifetime
{
    private readonly ProcessApiFixture fixture;

    public RegistrationJourneyTests(ProcessApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync()
    {
        fixture.DetachOutput();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AdminRegistration_CreatesUser_WhenNoAdminGrantMatches()
    {
        var userId = Guid.NewGuid();
        var email = $"{Guid.NewGuid():N}@uninvited.test";
        var registration = new CredentialRegisteredEvent(userId, email, ClientIds.Admin);

        await fixture.DispatchIntegrationEventAsync(
            registration,
            MessageEnvelope.Create<CredentialRegisteredEvent>(DateTimeOffset.UtcNow));

        var response = await fixture.CreateClient(userId, email).GetAsync("/api/auth/me");
        await response.ShouldBe(HttpStatusCode.OK);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(userId, payload.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(email, payload.RootElement.GetProperty("email").GetString());
        Assert.False(payload.RootElement.GetProperty("isAdmin").GetBoolean());
    }
}
