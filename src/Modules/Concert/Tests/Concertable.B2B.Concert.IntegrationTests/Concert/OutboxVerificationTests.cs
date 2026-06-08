using System.Net;
using System.Text.Json;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.Messaging.Contracts;
using Concertable.Messaging.Domain;
using Concertable.Messaging.Infrastructure.Outbox;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using static Concertable.B2B.Concert.IntegrationTests.Concert.ConcertRequestBuilders;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

[Collection("Integration")]
public sealed class OutboxVerificationTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public OutboxVerificationTests(ApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task PostConcert_WritesOutboxRow_AndDispatcherDrainsIt()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var concertId = fixture.SeedState.ConfirmedBooking.Concert!.Id;
        var expectedType = MessageTypeAttribute.Resolve(typeof(ConcertChangedEvent));

        // Act
        var response = await client.PutAsync($"/api/Concert/post/{concertId}", BuildPostRequest());

        // Assert — HTTP
        await response.ShouldBe(HttpStatusCode.NoContent);

        // Assert — outbox row was committed atomically with the concert write
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
        var row = await db.Set<OutboxMessageEntity>()
            .AsNoTracking()
            .SingleAsync(m => m.MessageType == expectedType);

        // Assert — dispatcher drains the row within 5 seconds
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (row.Status != OutboxStatus.Dispatched)
        {
            if (DateTimeOffset.UtcNow > deadline)
                Assert.Fail($"Outbox row {row.Id} was not dispatched within 5 s (status: {row.Status}).");

            await Task.Delay(200);

            using var pollScope = fixture.Services.CreateScope();
            var pollDb = pollScope.ServiceProvider.GetRequiredService<OutboxDbContext>();
            row = await pollDb.Set<OutboxMessageEntity>()
                .AsNoTracking()
                .SingleAsync(m => m.Id == row.Id);
        }
    }

    [Fact]
    public async Task PostVenueHireConcert_PublishesArtistAsTicketRevenuePayee()
    {
        // Arrange — run the full VenueHire accept flow so production code creates the draft
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync($"/api/Application/{fixture.SeedState.VenueHireApp.Id}/accept", (object?)null);
        await fixture.StripeClient.SendWebhookAsync();
        var concertResponse = await client.GetAsync($"/api/Concert/application/{fixture.SeedState.VenueHireApp.Id}");
        await concertResponse.ShouldBe(HttpStatusCode.OK);
        var concert = await concertResponse.Content.ReadAsync<ConcertDetailsResponse>();
        var expectedType = MessageTypeAttribute.Resolve(typeof(ConcertChangedEvent));

        // Act
        var response = await client.PutAsync($"/api/Concert/post/{concert!.Id}", BuildPostRequest());

        // Assert — the artist hired the venue upfront, so ticket revenue is paid to the artist
        await response.ShouldBe(HttpStatusCode.NoContent);
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
        var row = await db.Set<OutboxMessageEntity>()
            .AsNoTracking()
            .SingleAsync(m => m.MessageType == expectedType);
        using var payload = JsonDocument.Parse(row.Payload);
        Assert.Equal(fixture.SeedState.ArtistManager1.Id, payload.RootElement.GetProperty("payeeUserId").GetGuid());
    }
}
