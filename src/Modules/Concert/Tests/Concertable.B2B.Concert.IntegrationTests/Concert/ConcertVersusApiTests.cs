using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

[Collection("Integration")]

public sealed class ConcertVersusApiTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public ConcertVersusApiTests(ApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Finish_ShouldChargeGuaranteePlusDoorShareOffSession()
    {
        // Arrange
        var concert = fixture.SeedState.PastVersusBooking.Concert!;
        var contract = fixture.SeedState.PastVersusAppContract;
        var deferred = (DeferredBooking)fixture.SeedState.PastVersusBooking;

        // Act
        await fixture.FinishConcertAsync(concert.Id);

        // Assert — booking awaits the off-session settlement payment; completion happens on the webhook
        var payment = Assert.Single(fixture.ManagerPaymentClient.Payments);
        var venueTenantId = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == fixture.SeedState.VenueManager1.Id).Id;
        var artistTenantId = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == fixture.SeedState.ArtistManager1.Id).Id;
        Assert.Equal(venueTenantId, payment.PayerId);
        Assert.Equal(artistTenantId, payment.PayeeId);
        Assert.Equal(contract.CalculateArtistShare(concert.TicketsSold * concert.Price), payment.Amount);
        Assert.Equal(deferred.PaymentMethodId, payment.PaymentMethodId);
        Assert.Equal(deferred.Id, payment.BookingId);

        var application = await fixture.Applications.FirstAsync(a => a.Id == fixture.SeedState.PastVersusApp.Id);
        Assert.Equal(LifecycleState.AwaitingSettlement, application.State);
    }

    [Fact]
    public async Task Finish_ShouldCompleteBooking_WhenSettlementWebhookSucceeds()
    {
        // Arrange
        await fixture.FinishConcertAsync(fixture.SeedState.PastVersusBooking.Concert!.Id);

        // Act
        await fixture.StripeClient.SendWebhookAsync();

        // Assert
        var application = await fixture.Applications.FirstAsync(a => a.Id == fixture.SeedState.PastVersusApp.Id);
        Assert.Equal(LifecycleState.Complete, application.State);
    }
}
