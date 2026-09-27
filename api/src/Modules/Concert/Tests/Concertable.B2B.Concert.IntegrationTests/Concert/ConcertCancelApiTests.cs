using Concertable.B2B.Infrastructure.Payments;
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
        await fixture.PaymentSimulator.SendWebhookAsync();
        var booking = await GetBookingAsync(client, appId);

        var concertResponse = await fixture.GetConcertByApplicationAsync(client, appId);
        await concertResponse.ShouldBe(HttpStatusCode.OK);
        var concert = await concertResponse.Content.ReadAsync<OperationsResponse>();
        Assert.NotNull(concert!.Actions!.Cancel); // cancel offered while Booked

        // Act
        var cancelResponse = await client.PostAsync($"/api/concert/{concert.Id}/cancel");

        // Assert — booking dead, escrow refunded, cancel no longer offered.
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.CompleteLatestFinancialOperationAsync<RefundEscrowCommand>();
        var refund = fixture.PaymentTransport.SingleCommand<RefundEscrowCommand>();
        Assert.Equal(PaymentOperationReferences.Escrow(booking.BookingId), refund.Reference);
        Assert.Equal(RefundReasonCodes.RequestedByPayer, refund.Reason);
        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.Cancelled, persisted.State);

        var afterResponse = await client.GetAsync($"/api/concert/{concert.Id}/operations");
        var after = await afterResponse.Content.ReadAsync<OperationsResponse>();
        Assert.Null(after!.Actions!.Cancel);
    }

    [Fact]
    public async Task Cancel_ShouldRefundEscrowAndMarkCancelled_ForVenueHire()
    {
        // Arrange — VenueHire is prepaid; accept + webhook reaches Booked with escrow held.
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.VenueHireApp.Id;
        await client.PostAsync($"/api/application/{appId}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });
        await fixture.PaymentSimulator.SendWebhookAsync();
        var booking = await GetBookingAsync(client, appId);

        var concertResponse = await fixture.GetConcertByApplicationAsync(client, appId);
        await concertResponse.ShouldBe(HttpStatusCode.OK);
        var concert = await concertResponse.Content.ReadAsync<OperationsResponse>();
        // Act
        var cancelResponse = await client.PostAsync($"/api/concert/{concert!.Id}/cancel");

        // Assert
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.CompleteLatestFinancialOperationAsync<RefundEscrowCommand>();
        Assert.Equal(
            PaymentOperationReferences.Escrow(booking.BookingId),
            fixture.PaymentTransport.SingleCommand<RefundEscrowCommand>().Reference);
        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.Cancelled, persisted.State);
    }

    [Fact]
    public async Task Cancel_ShouldMarkCancelled_WhenTheRefundIsDeferred()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.VenueHireApp.Id;
        await client.PostAsync($"/api/application/{appId}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });
        await fixture.PaymentSimulator.SendWebhookAsync();
        var concertResponse = await fixture.GetConcertByApplicationAsync(client, appId);
        await concertResponse.ShouldBe(HttpStatusCode.OK);
        var concert = await concertResponse.Content.ReadAsync<OperationsResponse>();
        var cancelResponse = await client.PostAsync($"/api/concert/{concert!.Id}/cancel");
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);

        await fixture.DeferLatestFinancialOperationAsync<RefundEscrowCommand>();

        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.Cancelled, persisted.State);
    }

    [Fact]
    public async Task Cancel_ShouldMarkCancelled_ForDoorSplit_WhereNoEscrowIsHeld()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.DoorSplitApp.Id;
        await client.PostAsync($"/api/application/{appId}/checkout");
        var acceptResponse = await client.PostAsync($"/api/application/{appId}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        await fixture.PaymentSimulator.SendWebhookAsync();

        var concertResponse = await fixture.GetConcertByApplicationAsync(client, appId);
        await concertResponse.ShouldBe(HttpStatusCode.OK);
        var concert = await concertResponse.Content.ReadAsync<OperationsResponse>();

        // Act
        var cancelResponse = await client.PostAsync($"/api/concert/{concert!.Id}/cancel");

        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);
        Assert.Empty(await fixture.SettledFinancialCommandsAsync());
        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.Cancelled, persisted.State);
    }
    #region Cancel under concurrency

    [Fact]
    public async Task CancellationAndSettlement_WhenConcurrent_SerializeToOneTransition()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.PastDoorSplitBooking);
        await fixture.DeclareDoorRevenueAsync(concert.Id, 200m);
        await fixture.EnsureSupplierSelfBillingAgreementAsync(concert.Id);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var settlementTask = Task.Run(async () =>
        {
            await start.Task;
            return await fixture.CompleteConcertAsync(concert.Id);
        });
        var cancellationTask = Task.Run(async () =>
        {
            await start.Task;
            return await client.PostAsync($"/api/concert/{concert.Id}/cancel", (object?)null);
        });
        start.SetResult();
        var settlement = await settlementTask;
        var cancellation = await cancellationTask;

        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        if (persisted.State == ConcertState.Complete)
        {
            Assert.True(settlement.TryGetValue(out _));
            await cancellation.ShouldBe(HttpStatusCode.Conflict);
            Assert.NotNull(persisted.SettlementOperationId);
            Assert.Null(persisted.CancellationOperationId);
        }
        else
        {
            Assert.Equal(ConcertState.Cancelled, persisted.State);
            Assert.True(settlement.TryGetError(out var error));
            Assert.IsType<FinishConcertError.InvalidTransition>(error);
            await cancellation.ShouldBe(HttpStatusCode.NoContent);
            Assert.Null(persisted.SettlementOperationId);
            Assert.DoesNotContain(
                fixture.SettlementClient.Payments,
                value => value.Reference == PaymentOperationReferences.Settlement(concert.Id));
        }
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
        await fixture.PaymentSimulator.SendWebhookAsync();
        var concertResponse = await fixture.GetConcertByApplicationAsync(client, appId);
        await concertResponse.ShouldBe(HttpStatusCode.OK);
        var concert = await concertResponse.Content.ReadAsync<OperationsResponse>();
        Assert.NotNull(concert);
        var concertId = concert.Id;
        var competitor = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var responses = await RaceAsync(
            () => client.PostAsync($"/api/concert/{concertId}/cancel", (object?)null),
            () => competitor.PostAsync($"/api/concert/{concertId}/cancel", (object?)null));

        foreach (var response in responses)
            await response.ShouldBe(HttpStatusCode.NoContent);
        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concertId);
        Assert.Equal(ConcertState.CancellationPending, persisted.State);
        Assert.Single(
            await fixture.PaymentTransport.WaitForCommandsAsync<RefundEscrowCommand>(1),
            refund => refund.Reference == PaymentOperationReferences.Escrow(persisted.BookingId));
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
        Assert.Null(((DoorRevenueConcert)persisted).DoorRevenue);
    }

    [Fact]
    public async Task Cancel_ShouldSucceed_WhenCallerIsArtistPrincipal()
    {
        // Arrange — reach Booked as the venue, then have the artist attempt the cancel.
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        await venueClient.PostAsync($"/api/application/{appId}/checkout");
        await venueClient.PostAsync($"/api/application/{appId}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });
        await fixture.PaymentSimulator.SendWebhookAsync();
        var concert = await (await fixture.GetConcertByApplicationAsync(venueClient, appId))
            .Content.ReadAsync<OperationsResponse>();
        Assert.NotNull(concert);

        // Act
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var response = await artistClient.PostAsync($"/api/concert/{concert.Id}/cancel");

        await response.ShouldBe(HttpStatusCode.NoContent);
        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.CancellationPending, persisted.State);
    }

    [Fact]
    public async Task Cancel_ShouldReturn403_WhenCallerIsNotAPrincipal()
    {
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        await venueClient.PostAsync($"/api/application/{appId}/checkout");
        await venueClient.PostAsync(
            $"/api/application/{appId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await fixture.PaymentSimulator.SendWebhookAsync();
        var concert = await (await fixture.GetConcertByApplicationAsync(venueClient, appId))
            .Content.ReadAsync<OperationsResponse>();
        var unrelatedClient = fixture.CreateClient(fixture.SeedState.VenueManager2);

        var response = await unrelatedClient.PostAsync($"/api/concert/{concert!.Id}/cancel");

        await response.ShouldBe(HttpStatusCode.Forbidden);
        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(ConcertState.Draft, persisted.State);
    }

    private static async Task<BookingSummary> GetBookingAsync(HttpClient client, int applicationId)
    {
        var response = await client.GetAsync($"/api/booking/application/{applicationId}/summary");
        await response.ShouldBe(HttpStatusCode.OK);
        return Assert.IsType<BookingSummary>(await response.Content.ReadAsync<BookingSummary>());
    }

    private static async Task<HttpResponseMessage[]> RaceAsync(
        Func<Task<HttpResponseMessage>> first,
        Func<Task<HttpResponseMessage>> second)
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<HttpResponseMessage> RunAsync(Func<Task<HttpResponseMessage>> request)
        {
            await start.Task;
            return await request();
        }

        var firstTask = RunAsync(first);
        var secondTask = RunAsync(second);
        start.SetResult();
        return await Task.WhenAll(firstTask, secondTask);
    }
}
