using Concertable.B2B.Infrastructure.Payments;
using System.Net;
using Concertable.B2B.Concert.Domain.Lifecycle;
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
        var payment = Assert.Single(fixture.SettlementClient.Payments);
        var venueTenantId = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == fixture.SeedState.VenueManager1.Id).Id;
        var artistTenantId = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == fixture.SeedState.ArtistManager1.Id).Id;
        Assert.Equal(venueTenantId, payment.PayerId);
        Assert.Equal(artistTenantId, payment.PayeeId);
        Assert.Equal(280m, payment.Amount);
        Assert.Equal(concert.SettlementPaymentReference, payment.PaymentMethod);
        Assert.Equal(concert.Id, payment.ConcertId);

        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.Complete, persisted.State);
        Assert.Equal(payment.OperationId, persisted.SettlementOperationId);
        Assert.NotNull(await fixture.Invoices.SingleOrDefaultAsync(invoice => invoice.BookingId == concert.BookingId));
    }

    [Fact]
    public async Task Finish_WhenPersistenceFailsAfterPayment_RetryUsesTheSameOperation()
    {
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.PastDoorSplitBooking);
        await fixture.DeclareDoorRevenueAsync(concert.Id, DoorRevenue);
        await fixture.EnsureSupplierSelfBillingAgreementAsync(concert.Id);
        await fixture.FailSettlementPersistenceAsync();

        try
        {
            await Assert.ThrowsAnyAsync<DbUpdateException>(
                () => fixture.CompleteConcertAsync(concert.Id));
        }
        finally
        {
            await fixture.RestoreSettlementPersistenceAsync();
        }

        var interrupted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.AwaitingSettlement, interrupted.State);
        Assert.NotNull(interrupted.SettlementOperationId);

        await fixture.RunCompletionAsync();

        var payment = Assert.Single(
            fixture.SettlementClient.Payments,
            value => value.ConcertId == concert.Id);
        Assert.Equal(interrupted.SettlementOperationId, payment.OperationId);
        var settled = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.Complete, settled.State);
        Assert.Equal(1, await fixture.Invoices.CountAsync(invoice => invoice.BookingId == concert.BookingId));
    }

    [Fact]
    public async Task Finish_ShouldNotSettle_WhenDoorRevenueNotDeclared()
    {
        // Act — the completion sweep runs with no door revenue declared for the revenue-share gig
        await fixture.RunCompletionAsync();

        // Assert — the gig is skipped (no payout), still awaiting its declaration
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.PastDoorSplitBooking);
        Assert.DoesNotContain(fixture.SettlementClient.Payments, p => p.ConcertId == concert.Id);
        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.Posted, persisted.State);
    }

    [Fact]
    public async Task SettlementFailureAfterCompletion_IsIgnored()
    {
        var booking = fixture.SeedState.PastDoorSplitBooking;
        var concert = fixture.SeedState.ConcertFor(booking);
        await fixture.DeclareDoorRevenueAsync(concert.Id, DoorRevenue);
        await fixture.FinishConcertAsync(concert.Id);
        var completed = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);

        await fixture.SendSettlementFailedWebhookAsync(
            concert.Id,
            completed.SettlementOperationId!.Value);
        await fixture.PaymentSimulator.SendWebhookAsync();

        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.Complete, persisted.State);
    }

    [Fact]
    public async Task Cancel_ShouldReturnConflict_WhenSettlementFailed()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var booking = fixture.SeedState.PastDoorSplitBooking;
        var concert = fixture.SeedState.ConcertFor(booking);
        await fixture.DeclareDoorRevenueAsync(concert.Id, DoorRevenue);
        await fixture.EnsureSupplierSelfBillingAgreementAsync(concert.Id);
        await fixture.FailSettlementPersistenceAsync();
        try
        {
            await Assert.ThrowsAnyAsync<DbUpdateException>(
                () => fixture.CompleteConcertAsync(concert.Id));
        }
        finally
        {
            await fixture.RestoreSettlementPersistenceAsync();
        }
        var awaiting = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        await fixture.SendSettlementFailedWebhookAsync(
            concert.Id,
            awaiting.SettlementOperationId!.Value);

        var response = await client.PostAsync($"/api/concert/{concert.Id}/cancel");

        await response.ShouldBe(HttpStatusCode.Conflict);
        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.SettlementFailed, persisted.State);
    }

    [Fact]
    public async Task Finish_ShouldIgnoreDuplicateSettlementWebhookEvent()
    {
        // Arrange
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.PastDoorSplitBooking);
        await fixture.DeclareDoorRevenueAsync(concert.Id, DoorRevenue);
        await fixture.FinishConcertAsync(concert.Id);

        // Act
        await fixture.PaymentSimulator.SendWebhookAsync();
        await fixture.PaymentSimulator.SendWebhookAsync();

        // Assert
        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.Complete, persisted.State);
    }
}
