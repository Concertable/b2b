using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.IntegrationTests.Fixtures;
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
        var concert = fixture.SeedState.PastDoorSplitBooking.Concert!;
        var deal = fixture.SeedState.PastDoorSplitAppDeal;
        var deferred = (DeferredBooking)fixture.SeedState.PastDoorSplitBooking;
        await fixture.DeclareDoorRevenueAsync(concert.Id, DoorRevenue);

        // Act
        await fixture.FinishConcertAsync(concert.Id);

        // Assert — booking awaits the off-session settlement payment; completion happens on the webhook
        var payment = Assert.Single(fixture.ManagerPaymentClient.Payments);
        var venueTenantId = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == fixture.SeedState.VenueManager1.Id).Id;
        var artistTenantId = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == fixture.SeedState.ArtistManager1.Id).Id;
        Assert.Equal(venueTenantId, payment.PayerId);
        Assert.Equal(artistTenantId, payment.PayeeId);
        Assert.Equal(deal.CalculateArtistShare(DoorRevenue), payment.Amount);
        Assert.Equal(deferred.PaymentMethodId, payment.PaymentMethodId);
        Assert.Equal(deferred.Id, payment.BookingId);

        var application = await fixture.ConcertReads.Set<ApplicationEntity>().FirstAsync(a => a.Id == fixture.SeedState.PastDoorSplitApp.Id);
        Assert.Equal(LifecycleState.AwaitingSettlement, application.State);
    }

    [Fact]
    public async Task Finish_ShouldNotSettle_WhenDoorRevenueNotDeclared()
    {
        // Act — the completion sweep runs with no door revenue declared for the revenue-share gig
        await fixture.RunCompletionAsync();

        // Assert — the gig is skipped (no payout), still awaiting its declaration
        Assert.DoesNotContain(fixture.ManagerPaymentClient.Payments, p => p.BookingId == fixture.SeedState.PastDoorSplitBooking.Id);
        var application = await fixture.ConcertReads.Set<ApplicationEntity>().FirstAsync(a => a.Id == fixture.SeedState.PastDoorSplitApp.Id);
        Assert.Equal(LifecycleState.Booked, application.State);
    }

    [Fact]
    public async Task Finish_ShouldCompleteBooking_WhenSettlementWebhookSucceeds()
    {
        // Arrange
        var concert = fixture.SeedState.PastDoorSplitBooking.Concert!;
        await fixture.DeclareDoorRevenueAsync(concert.Id, DoorRevenue);
        await fixture.FinishConcertAsync(concert.Id);

        // Act
        await fixture.StripeClient.SendWebhookAsync();

        // Assert
        var application = await fixture.ConcertReads.Set<ApplicationEntity>().FirstAsync(a => a.Id == fixture.SeedState.PastDoorSplitApp.Id);
        Assert.Equal(LifecycleState.Complete, application.State);
    }

    [Fact]
    public async Task Finish_ShouldIgnoreDuplicateSettlementWebhookEvent()
    {
        // Arrange
        var concert = fixture.SeedState.PastDoorSplitBooking.Concert!;
        await fixture.DeclareDoorRevenueAsync(concert.Id, DoorRevenue);
        await fixture.FinishConcertAsync(concert.Id);

        // Act
        await fixture.StripeClient.SendWebhookAsync();
        await fixture.StripeClient.SendWebhookAsync();

        // Assert
        var application = await fixture.ConcertReads.Set<ApplicationEntity>().FirstAsync(a => a.Id == fixture.SeedState.PastDoorSplitApp.Id);
        Assert.Equal(LifecycleState.Complete, application.State);
    }
}
