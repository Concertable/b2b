using System.Net;
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
public class OutboxVerificationTests : IAsyncLifetime
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
}
