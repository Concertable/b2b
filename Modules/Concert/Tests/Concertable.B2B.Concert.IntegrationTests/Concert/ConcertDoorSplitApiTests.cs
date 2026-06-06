using Concertable.B2B.Concert.Domain.Enums;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

[Collection("Integration")]

public sealed class ConcertDoorSplitApiTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public ConcertDoorSplitApiTests(ApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Finish_ShouldChargeArtistDoorShareOffSession()
    {
        // Arrange
        var concert = fixture.SeedState.PastDoorSplitBooking.Concert!;
        var contract = fixture.SeedState.PastDoorSplitAppContract;
        var deferred = (DeferredBooking)fixture.SeedState.PastDoorSplitBooking;

        // Act
        await fixture.FinishConcertAsync(concert.Id);

        // Assert — booking awaits the off-session settlement payment; completion happens on the webhook
        var payment = Assert.Single(fixture.ManagerPaymentClient.Payments);
        Assert.Equal(fixture.SeedState.VenueManager1.Id, payment.PayerId);
        Assert.Equal(fixture.SeedState.ArtistManager1.Id, payment.PayeeId);
        Assert.Equal(contract.CalculateArtistShare(concert.TicketsSold * concert.Price), payment.Amount);
        Assert.Equal(deferred.PaymentMethodId, payment.PaymentMethodId);
        Assert.Equal(deferred.Id, payment.BookingId);

        var booking = await fixture.ReadDbContext.Bookings.FirstAsync(b => b.Id == deferred.Id);
        Assert.Equal(BookingStatus.AwaitingPayment, booking.Status);
        var finished = await fixture.ReadDbContext.Concerts.FirstAsync(c => c.Id == concert.Id);
        Assert.Equal(ConcertStage.Finished, finished.CurrentStage);
    }

    [Fact]
    public async Task Finish_ShouldCompleteBooking_WhenSettlementWebhookSucceeds()
    {
        // Arrange
        await fixture.FinishConcertAsync(fixture.SeedState.PastDoorSplitBooking.Concert!.Id);

        // Act
        await fixture.StripeClient.SendWebhookAsync();

        // Assert
        var booking = await fixture.ReadDbContext.Bookings.FirstAsync(b => b.Id == fixture.SeedState.PastDoorSplitBooking.Id);
        Assert.Equal(BookingStatus.Complete, booking.Status);
    }

    [Fact]
    public async Task Finish_ShouldIgnoreDuplicateSettlementWebhookEvent()
    {
        // Arrange
        await fixture.FinishConcertAsync(fixture.SeedState.PastDoorSplitBooking.Concert!.Id);

        // Act
        await fixture.StripeClient.SendWebhookAsync();
        await fixture.StripeClient.SendWebhookAsync();

        // Assert
        var booking = await fixture.ReadDbContext.Bookings.FirstAsync(b => b.Id == fixture.SeedState.PastDoorSplitBooking.Id);
        Assert.Equal(BookingStatus.Complete, booking.Status);
    }
}
