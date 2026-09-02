using System.Net;
using System.Text.Json;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.Messaging.Contracts;
using Xunit.Abstractions;

namespace Concertable.B2B.Lifecycle.IntegrationTests;

[Collection("Integration")]
public sealed class ConcertPostingLifecycleTests : IAsyncLifetime
{
    private readonly LifecycleApiFixture fixture;

    public ConcertPostingLifecycleTests(LifecycleApiFixture fixture, ITestOutputHelper output)
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
    public async Task PostConcert_WritesOutboxRow_AndOutboxDrainsIt()
    {
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.ConfirmedBooking);
        var venue = fixture.SeedState.Venues.Single(value => value.Id == concert.VenueId);
        var manager = fixture.SeedState.VenueManagers.Single(value => value.Id == venue.UserId);
        var client = fixture.CreateClient(manager);
        var expectedType = MessageTypeAttribute.Resolve(typeof(ConcertChangedEvent));

        var response = await client.PutAsync($"/api/concert/post/{concert.Id}", BuildPostRequest());

        await response.ShouldBe(HttpStatusCode.NoContent);
        var row = await fixture.GetOutboxMessageAsync(expectedType);
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (!row.IsDispatched)
        {
            if (DateTimeOffset.UtcNow > deadline)
                Assert.Fail($"Outbox row {row.Id} was not dispatched within 5 s.");

            await Task.Delay(200);
            row = await fixture.GetOutboxMessageAsync(row.Id);
        }
    }

    [Fact]
    public async Task PostVenueHireConcert_PublishesArtistAsTicketRevenuePayee()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync(
            $"/api/application/{fixture.SeedState.VenueHireApp.Id}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await fixture.StripeClient.SendWebhookAsync();
        var concertResponse = await client.GetAsync(
            $"/api/concert/application/{fixture.SeedState.VenueHireApp.Id}");
        await concertResponse.ShouldBe(HttpStatusCode.OK);
        using var concertPayload = JsonDocument.Parse(await concertResponse.Content.ReadAsStringAsync());
        var concertId = concertPayload.RootElement.GetProperty("id").GetInt32();
        var expectedType = MessageTypeAttribute.Resolve(typeof(ConcertChangedEvent));

        var response = await client.PutAsync($"/api/concert/post/{concertId}", BuildPostRequest());

        await response.ShouldBe(HttpStatusCode.NoContent);
        var row = await fixture.GetOutboxMessageAsync(expectedType);
        using var payload = JsonDocument.Parse(row.Payload);
        Assert.Equal(
            fixture.SeedState.ArtistManager1.Id,
            payload.RootElement.GetProperty("payeeUserId").GetGuid());
    }

    private static object BuildPostRequest() => new
    {
        name = "Test Concert",
        about = "Test Concert About",
        price = 10.00m,
        totalTickets = 100
    };
}
