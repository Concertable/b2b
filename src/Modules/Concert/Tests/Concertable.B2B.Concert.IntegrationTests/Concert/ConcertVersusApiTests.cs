using Concertable.B2B.Concert.Domain.State;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

[Collection("Integration")]

public sealed class ConcertVersusApiTests : IAsyncLifetime
{
    private const decimal DoorRevenue = 200m;

    private readonly ConcertApiFixture fixture;

    public ConcertVersusApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Finish_ShouldChargeGuaranteePlusDoorShareOffSession_AfterDoorRevenueDeclared()
    {
        // Arrange — the venue declares the door revenue; Versus settles guarantee + a % of it
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.PastVersusBooking);
        await fixture.DeclareDoorRevenueAsync(concert.Id, DoorRevenue);

        // Act
        await fixture.FinishConcertAsync(concert.Id);

        // Assert — booking awaits the off-session settlement payment; completion happens on the webhook
        var payment = Assert.Single(fixture.ManagerPaymentClient.Payments);
        var venueTenantId = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == fixture.SeedState.VenueManager1.Id).Id;
        var artistTenantId = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == fixture.SeedState.ArtistManager1.Id).Id;
        Assert.Equal(venueTenantId, payment.PayerId);
        Assert.Equal(artistTenantId, payment.PayeeId);
        Assert.Equal(254m, payment.Amount);
        Assert.Equal(concert.SettlementPaymentMethodId, payment.PaymentMethodId);
        Assert.Equal(concert.BookingId, payment.BookingId);

        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.AwaitingSettlement, persisted.State);
    }

    [Fact]
    public async Task Finish_ShouldCompleteBooking_WhenSettlementWebhookSucceeds()
    {
        // Arrange
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.PastVersusBooking);
        await fixture.DeclareDoorRevenueAsync(concert.Id, DoorRevenue);
        await fixture.FinishConcertAsync(concert.Id);

        // Act
        await fixture.StripeClient.SendWebhookAsync();

        // Assert
        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.Complete, persisted.State);
    }
}
