using Concertable.B2B.Concert.Domain.State;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

[Collection("Integration")]

public sealed class ConcertDoorSplitApiTests : IAsyncLifetime
{
    private const decimal DoorRevenue = 200m;

    private readonly ConcertApiFixture fixture;

    public ConcertDoorSplitApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Finish_ShouldChargeArtistDoorShareOffSession_AfterDoorRevenueDeclared()
    {
        // Arrange — the venue declares the night's door revenue; settlement is a % of that
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.PastDoorSplitBooking);
        await fixture.DeclareDoorRevenueAsync(concert.Id, DoorRevenue);

        // Act
        await fixture.FinishConcertAsync(concert.Id);

        // Assert — booking awaits the off-session settlement payment; completion happens on the webhook
        var payment = Assert.Single(fixture.ManagerPaymentClient.Payments);
        var venueTenantId = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == fixture.SeedState.VenueManager1.Id).Id;
        var artistTenantId = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == fixture.SeedState.ArtistManager1.Id).Id;
        Assert.Equal(venueTenantId, payment.PayerId);
        Assert.Equal(artistTenantId, payment.PayeeId);
        Assert.Equal(280m, payment.Amount);
        Assert.Equal(concert.SettlementPaymentMethodId, payment.PaymentMethodId);
        Assert.Equal(concert.BookingId, payment.BookingId);

        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.AwaitingSettlement, persisted.State);
    }

    [Fact]
    public async Task Finish_ShouldNotSettle_WhenDoorRevenueNotDeclared()
    {
        // Act — the completion sweep runs with no door revenue declared for the revenue-share gig
        await fixture.RunCompletionAsync();

        // Assert — the gig is skipped (no payout), still awaiting its declaration
        Assert.DoesNotContain(fixture.ManagerPaymentClient.Payments, p => p.BookingId == fixture.SeedState.PastDoorSplitBooking.Id);
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.PastDoorSplitBooking);
        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.Draft, persisted.State);
    }

    [Fact]
    public async Task Finish_ShouldCompleteBooking_WhenFailedSettlementIsRetriedSuccessfully()
    {
        // Arrange
        var booking = fixture.SeedState.PastDoorSplitBooking;
        var concert = fixture.SeedState.ConcertFor(booking);
        await fixture.DeclareDoorRevenueAsync(concert.Id, DoorRevenue);
        await fixture.FinishConcertAsync(concert.Id);

        // Act
        await fixture.SendSettlementFailedWebhookAsync(booking.Id);
        await fixture.StripeClient.SendWebhookAsync();

        // Assert
        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.Complete, persisted.State);
    }

    [Fact]
    public async Task Finish_ShouldIgnoreDuplicateSettlementWebhookEvent()
    {
        // Arrange
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.PastDoorSplitBooking);
        await fixture.DeclareDoorRevenueAsync(concert.Id, DoorRevenue);
        await fixture.FinishConcertAsync(concert.Id);

        // Act
        await fixture.StripeClient.SendWebhookAsync();
        await fixture.StripeClient.SendWebhookAsync();

        // Assert
        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.Complete, persisted.State);
    }
}
