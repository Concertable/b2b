using System.Net;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Microsoft.EntityFrameworkCore;
using Concertable.Payment.Contracts;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

[Collection("Integration")]

public sealed class ConcertCancelApiTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public ConcertCancelApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Cancel_ShouldRefundEscrowAndMarkCancelled_ForFlatFee()
    {
        // Arrange — drive the FlatFee booking to Booked (escrow held).
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        await client.PostAsync($"/api/application/{appId}/checkout");
        var acceptResponse = await client.PostAsync($"/api/application/{appId}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();
        var booking = await GetBookingAsync(client, appId);

        var concertResponse = await client.GetAsync($"/api/concert/application/{appId}");
        await concertResponse.ShouldBe(HttpStatusCode.OK);
        var concert = await concertResponse.Content.ReadAsync<MyDetailsResponse>();
        Assert.NotNull(concert!.Actions!.Cancel); // cancel offered while Booked

        // Act
        var cancelResponse = await client.PostAsync($"/api/concert/{concert.Id}/cancel");

        // Assert — booking dead, escrow refunded, cancel no longer offered.
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.CompleteLatestFinancialOperationAsync<RefundEscrowCommand>();
        var refund = fixture.PaymentTransport.SingleCommand<RefundEscrowCommand>();
        Assert.Equal(booking.BookingId, refund.BookingId);
        Assert.Equal(RefundReasonCodes.RequestedByCustomer, refund.Reason);
        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.Cancelled, persisted.State);

        var afterResponse = await client.GetAsync($"/api/concert/application/{appId}");
        var after = await afterResponse.Content.ReadAsync<MyDetailsResponse>();
        Assert.Null(after!.Actions!.Cancel);
    }

    [Fact]
    public async Task Cancel_ShouldRefundEscrowAndMarkCancelled_ForVenueHire()
    {
        // Arrange — VenueHire is prepaid; accept + webhook reaches Booked with escrow held.
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.VenueHireApp.Id;
        await client.PostAsync($"/api/application/{appId}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });
        await fixture.StripeClient.SendWebhookAsync();
        var booking = await GetBookingAsync(client, appId);

        var concertResponse = await client.GetAsync($"/api/concert/application/{appId}");
        await concertResponse.ShouldBe(HttpStatusCode.OK);
        var concert = await concertResponse.Content.ReadAsync<MyDetailsResponse>();
        // Act
        var cancelResponse = await client.PostAsync($"/api/concert/{concert!.Id}/cancel");

        // Assert
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.CompleteLatestFinancialOperationAsync<RefundEscrowCommand>();
        Assert.Equal(booking.BookingId, fixture.PaymentTransport.SingleCommand<RefundEscrowCommand>().BookingId);
        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.Cancelled, persisted.State);
    }

    [Fact]
    public async Task Cancel_ShouldMarkCancelled_ForDoorSplit_WhereNoEscrowIsHeld()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.DoorSplitApp.Id;
        await client.PostAsync($"/api/application/{appId}/checkout");
        var acceptResponse = await client.PostAsync($"/api/application/{appId}/accept", new { eSignature = new { signatoryName = "Test Signatory" }, paymentMethodId = "pm_card_visa" });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();

        var concertResponse = await client.GetAsync($"/api/concert/application/{appId}");
        await concertResponse.ShouldBe(HttpStatusCode.OK);
        var concert = await concertResponse.Content.ReadAsync<MyDetailsResponse>();

        // Act
        var cancelResponse = await client.PostAsync($"/api/concert/{concert!.Id}/cancel");

        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);
        Assert.DoesNotContain(fixture.PaymentTransport.Commands, command => command is RefundEscrowCommand);
        Assert.Empty(fixture.EscrowClient.Holds);
        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.Cancelled, persisted.State);
    }
    #region Cancel under concurrency

    [Fact]
    public async Task Cancel_WhenSettlementReservationWinsTheRace_ReturnsConflictAndLeavesTheConcertSettled()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.PastDoorSplitBooking);
        await fixture.DeclareDoorRevenueAsync(concert.Id, 200m);
        await fixture.EnsureSupplierSelfBillingAgreementAsync(concert.Id);
        fixture.ArmConcertConflict(async () =>
        {
            var settlement = await fixture.CompleteConcertAsync(concert.Id);
            Assert.True(settlement.TryGetValue(out _));
        });

        var cancellation = await client.PostAsync($"/api/concert/{concert.Id}/cancel", (object?)null);

        await cancellation.ShouldBe(HttpStatusCode.Conflict);
        Assert.Equal(1, fixture.Conflicts.ForcedConflicts);
        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.Complete, persisted.State);
        Assert.NotNull(persisted.SettlementOperationId);
        Assert.Null(persisted.CancellationOperationId);
        Assert.Single(
            fixture.ManagerPaymentClient.Payments,
            value => value.BookingId == concert.BookingId);
    }

    [Fact]
    public async Task Settle_WhenCancellationWinsTheRace_ReturnsConflictAndLeavesTheConcertCancelling()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.PastDoorSplitBooking);
        await fixture.DeclareDoorRevenueAsync(concert.Id, 200m);
        await fixture.EnsureSupplierSelfBillingAgreementAsync(concert.Id);
        fixture.ArmConcertConflict(async () =>
        {
            var cancellation = await client.PostAsync($"/api/concert/{concert.Id}/cancel", (object?)null);
            await cancellation.ShouldBe(HttpStatusCode.NoContent);
        });

        var settlement = await fixture.CompleteConcertAsync(concert.Id);

        Assert.True(settlement.TryGetError(out var error));
        Assert.IsType<FinishConcertError.InvalidTransition>(error);
        Assert.Equal(1, fixture.Conflicts.ForcedConflicts);
        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.Cancelled, persisted.State);
        Assert.Null(persisted.SettlementOperationId);
        Assert.DoesNotContain(
            fixture.ManagerPaymentClient.Payments,
            value => value.BookingId == concert.BookingId);
    }

    [Fact]
    public async Task Cancel_WhenAnotherCancellationWinsTheRace_SucceedsWithoutASecondRefund()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        await client.PostAsync($"/api/application/{appId}/checkout");
        var acceptResponse = await client.PostAsync(
            $"/api/application/{appId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();
        var concertResponse = await client.GetAsync($"/api/concert/application/{appId}");
        await concertResponse.ShouldBe(HttpStatusCode.OK);
        var concert = await concertResponse.Content.ReadAsync<MyDetailsResponse>();
        var competitor = fixture.CreateClient(fixture.SeedState.VenueManager1);
        fixture.ArmConcertConflict(async () =>
        {
            var winner = await competitor.PostAsync($"/api/concert/{concert!.Id}/cancel", (object?)null);
            await winner.ShouldBe(HttpStatusCode.NoContent);
        });

        var loser = await client.PostAsync($"/api/concert/{concert!.Id}/cancel", (object?)null);

        await loser.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(1, fixture.Conflicts.ForcedConflicts);
        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.CancellationPending, persisted.State);
        Assert.Single(
            fixture.PaymentTransport.Commands,
            command => command is RefundEscrowCommand refund && refund.BookingId == persisted.BookingId);
    }

    #endregion


    [Fact]
    public async Task DeclareDoorRevenue_ShouldReturnConflict_AfterCancellation()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.PastDoorSplitBooking);

        var cancelResponse = await client.PostAsync($"/api/concert/{concert.Id}/cancel");
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);

        var response = await client.PostAsync(
            $"/api/concert/{concert.Id}/door-revenue",
            new { doorRevenue = 200m });

        await response.ShouldBe(HttpStatusCode.Conflict);
        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.Cancelled, persisted.State);
        Assert.Null(persisted.DoorRevenue);
    }

    [Fact]
    public async Task Cancel_ShouldReturn403_WhenCallerIsArtist()
    {
        // Arrange — reach Booked as the venue, then have the artist attempt the cancel.
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        await venueClient.PostAsync($"/api/application/{appId}/checkout");
        await venueClient.PostAsync($"/api/application/{appId}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });
        await fixture.StripeClient.SendWebhookAsync();
        var concert = await (await venueClient.GetAsync($"/api/concert/application/{appId}")).Content.ReadAsync<MyDetailsResponse>();

        // Act
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var response = await artistClient.PostAsync($"/api/concert/{concert!.Id}/cancel");

        // Assert — cancelling is a venue decision; the artist lacks the permission.
        await response.ShouldBe(HttpStatusCode.Forbidden);
        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.Draft, persisted.State);
    }

    private static async Task<BookingSummary> GetBookingAsync(HttpClient client, int applicationId)
    {
        var response = await client.GetAsync($"/api/booking/application/{applicationId}");
        await response.ShouldBe(HttpStatusCode.OK);
        return Assert.IsType<BookingSummary>(await response.Content.ReadAsync<BookingSummary>());
    }
}
