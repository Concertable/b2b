using Concertable.B2B.Infrastructure.Payments;
using System.Net;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Booking.Contracts.Events;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Concertable.B2B.Booking.IntegrationTests;

[Collection("Integration")]
public sealed class BookingCancellationApiTests : IAsyncLifetime
{
    private readonly BookingApiFixture fixture;

    public BookingCancellationApiTests(BookingApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Cancel_ShouldRefundEscrowAndMarkCancelled_FromAwaitingConfirmation()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptFlatFeeAsync(client);

        var response = await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);

        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(BookingState.CancellationPending, await StateOfAsync(bookingId));
        var refund = Assert.Single(
            await fixture.PaymentTransport.WaitForCommandsAsync<RefundEscrowCommand>(1));
        Assert.Equal(PaymentOperationReferences.Escrow(bookingId), refund.Reference);
        Assert.Equal(RefundReasonCodes.RequestedByPayer, refund.Reason);
        await fixture.CompleteLatestFinancialOperationAsync<RefundEscrowCommand>();
        Assert.Equal(BookingState.Cancelled, await StateOfAsync(bookingId));
    }

    [Fact]
    public async Task Cancel_ShouldMarkCancelled_WithoutHeldEscrow()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptDoorSplitAsync(client);

        var response = await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);

        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.Empty(await fixture.SettledFinancialCommandsAsync());
        Assert.Equal(BookingState.Cancelled, await StateOfAsync(bookingId));
    }

    [Fact]
    public async Task Cancel_ShouldMarkCancelled_FromConfirmationFailed()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptVenueHireAsync(client);
        await fixture.SendEscrowFailedWebhookAsync(bookingId);
        Assert.Equal(BookingState.ConfirmationFailed, await StateOfAsync(bookingId));

        var response = await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);

        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.DoesNotContain(await fixture.SettledFinancialCommandsAsync(), command => command is RefundEscrowCommand);
        Assert.Equal(BookingState.Cancelled, await StateOfAsync(bookingId));
    }

    [Fact]
    public async Task Cancel_ShouldComplete_WhenEscrowRejectionLandsAfterCancellation()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptFlatFeeAsync(client);
        var cancelResponse = await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(BookingState.CancellationPending, await StateOfAsync(bookingId));

        await fixture.SendEscrowFailedWebhookAsync(bookingId);

        Assert.Equal(BookingState.Cancelled, await StateOfAsync(bookingId));
    }

    [Fact]
    public async Task Cancel_ShouldRefundAgainAndStayCancelled_WhenEscrowCaptureLandsAfterCancel()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptVenueHireAsync(client);
        var cancelResponse = await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);

        await fixture.PaymentSimulator.SendWebhookAsync();
        var refunds = await fixture.PaymentTransport.WaitForCommandsAsync<RefundEscrowCommand>(2);
        await fixture.CompleteLatestFinancialOperationAsync<RefundEscrowCommand>();

        Assert.Equal(BookingState.Cancelled, await StateOfAsync(bookingId));
        Assert.Equal(2, refunds.Count(command => command.Reference == PaymentOperationReferences.Escrow(bookingId)));
    }

    [Fact]
    public async Task Cancel_ShouldWaitForTheCapture_WhenTheRefundIsDeferred()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptFlatFeeAsync(client);
        var cancelResponse = await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);

        await fixture.DeferLatestFinancialOperationAsync<RefundEscrowCommand>();
        Assert.Equal(BookingState.CancellationPending, await StateOfAsync(bookingId));

        await fixture.PaymentSimulator.SendWebhookAsync();
        var refunds = await fixture.PaymentTransport.WaitForCommandsAsync<RefundEscrowCommand>(2);
        await fixture.CompleteLatestFinancialOperationAsync<RefundEscrowCommand>();

        Assert.Equal(BookingState.Cancelled, await StateOfAsync(bookingId));
        Assert.Single(refunds.Select(command => command.OperationId).Distinct());
        Assert.Equal(0, await fixture.GetConcertCountAsync(bookingId));
    }

    [Fact]
    public async Task Cancel_ShouldComplete_WhenTheCaptureFailsAfterTheRefundIsDeferred()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptFlatFeeAsync(client);
        var cancelResponse = await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);

        await fixture.DeferLatestFinancialOperationAsync<RefundEscrowCommand>();
        Assert.Equal(BookingState.CancellationPending, await StateOfAsync(bookingId));

        await fixture.RejectLatestFinancialOperationAsync<CaptureEscrowCommand>();

        Assert.Equal(BookingState.Cancelled, await StateOfAsync(bookingId));
        Assert.Equal(0, await fixture.GetConcertCountAsync(bookingId));
    }

    [Fact]
    public async Task Cancel_ShouldStayCancelled_WhenSecondRefundIsRejectedAfterCancel()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptVenueHireAsync(client);
        var cancelResponse = await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);

        await fixture.PaymentSimulator.SendWebhookAsync();
        var refund = (await fixture.PaymentTransport.WaitForCommandsAsync<RefundEscrowCommand>(2)).Last();
        await fixture.CompleteLatestFinancialOperationAsync<RefundEscrowCommand>();
        Assert.Equal(BookingState.Cancelled, await StateOfAsync(bookingId));

        await fixture.DispatchIntegrationEventAsync(
            new RefundEscrowRejectedEvent(refund.OperationId, refund.Reference, "refund_failed", "Refund failed"),
            MessageEnvelope.Create<RefundEscrowRejectedEvent>(fixture.SeedNow));

        Assert.Equal(BookingState.Cancelled, await StateOfAsync(bookingId));
    }

    [Fact]
    public async Task Cancel_ShouldRecordCancellationFailure_WhenRefundIsRejected()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptFlatFeeAsync(client);
        var cancelResponse = await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);

        await fixture.RejectLatestFinancialOperationAsync<RefundEscrowCommand>();

        var entity = await fixture.Bookings.SingleAsync(value => value.Id == bookingId);
        Assert.Equal(BookingState.CancellationFailed, entity.State);
        Assert.Equal("refund_failed", entity.FinancialFailure!.Code);
        Assert.Equal("Refund failed", entity.FinancialFailure!.Message);
    }

    [Fact]
    public async Task Cancel_ShouldReturn409_WhenConfirmed()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptFlatFeeAsync(client);
        await fixture.PaymentSimulator.SendWebhookAsync();
        Assert.Equal(BookingState.Confirmed, await StateOfAsync(bookingId));

        var response = await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);

        await response.ShouldBe(HttpStatusCode.Conflict);
        Assert.Equal(BookingState.Confirmed, await StateOfAsync(bookingId));
    }

    #region Cancel under concurrency

    [Fact]
    public async Task ConcurrentCancellation_QueuesExactlyOneRefund()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptFlatFeeAsync(client);
        var competitor = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var responses = await RaceAsync(
            () => client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null),
            () => competitor.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null));

        foreach (var response in responses)
            await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(BookingState.CancellationPending, await StateOfAsync(bookingId));
        var refund = Assert.Single(
            await fixture.PaymentTransport.WaitForCommandsAsync<RefundEscrowCommand>(1));
        Assert.Equal(PaymentOperationReferences.Escrow(bookingId), refund.Reference);
        Assert.Single(
            fixture.PaymentTransport.Commands,
            command => command is RefundEscrowCommand queued
                && queued.Reference == PaymentOperationReferences.Escrow(bookingId));
    }

    [Fact]
    public async Task ConcurrentCancellation_PublishesOneCancellationEvent()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptDoorSplitAsync(client);
        var competitor = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var responses = await RaceAsync(
            () => client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null),
            () => competitor.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null));

        foreach (var response in responses)
            await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(BookingState.Cancelled, await StateOfAsync(bookingId));
        Assert.Equal(1, await fixture.GetOutboxMessageCountAsync<BookingCancelledEvent>());
    }

    [Fact]
    public async Task CancellationAndConfirmation_WhenConcurrent_SerializeToOneTransition()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptFlatFeeAsync(client);
        var capture = await fixture.PaymentTransport.SingleCommandAsync<CaptureEscrowCommand>();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationTask = Task.Run(async () =>
        {
            await start.Task;
            return await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);
        });
        var confirmationTask = Task.Run(async () =>
        {
            await start.Task;
            await fixture.DispatchIntegrationEventAsync(
                new CaptureEscrowSucceededEvent(capture.OperationId, capture.Reference),
                MessageEnvelope.Create<CaptureEscrowSucceededEvent>(fixture.SeedNow));
        });
        start.SetResult();
        await confirmationTask;
        var cancellation = await cancellationTask;

        var booking = await fixture.Bookings.SingleAsync(value => value.Id == bookingId);
        if (booking.State == BookingState.Confirmed)
        {
            await cancellation.ShouldBe(HttpStatusCode.Conflict);
            Assert.Null(booking.CancellationOperationId);
            Assert.Equal(1, await fixture.GetConcertCountAsync(bookingId));
        }
        else
        {
            await cancellation.ShouldBe(HttpStatusCode.NoContent);
            Assert.Equal(BookingState.CancellationPending, booking.State);
            Assert.Equal(0, await fixture.GetConcertCountAsync(bookingId));
            Assert.Contains(
                await fixture.PaymentTransport.WaitForCommandsAsync<RefundEscrowCommand>(1),
                refund => refund.Reference == PaymentOperationReferences.Escrow(bookingId));
        }
    }

    [Fact]
    public async Task CancellationAndPaymentVerification_WhenConcurrent_SerializeToOneTransition()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var applicationId = fixture.SeedState.DoorSplitApp.Id;
        var bookingId = await AcceptDoorSplitAsync(client);
        var verified = new VerifyPaymentSucceededDomainEvent(
            new VerifyPaymentSucceeded(applicationId));
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationTask = Task.Run(async () =>
        {
            await start.Task;
            return await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);
        });
        var verificationTask = Task.Run(async () =>
        {
            await start.Task;
            await fixture.DispatchPreCommitDomainEventAsync(verified);
        });
        start.SetResult();
        await verificationTask;
        var cancellation = await cancellationTask;

        var state = await StateOfAsync(bookingId);
        if (state == BookingState.Confirmed)
        {
            await cancellation.ShouldBe(HttpStatusCode.Conflict);
            Assert.Equal(1, await fixture.GetConcertCountAsync(bookingId));
        }
        else
        {
            await cancellation.ShouldBe(HttpStatusCode.NoContent);
            Assert.Equal(BookingState.Cancelled, state);
            Assert.Equal(0, await fixture.GetConcertCountAsync(bookingId));
        }
    }

    [Fact]
    public async Task Cancel_WhenAlreadyPending_SucceedsWithoutASecondRefund()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptFlatFeeAsync(client);
        var first = await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);
        await first.ShouldBe(HttpStatusCode.NoContent);

        var duplicate = await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);

        await duplicate.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(BookingState.CancellationPending, await StateOfAsync(bookingId));
        var refunds = await fixture.PaymentTransport.WaitForCommandsAsync<RefundEscrowCommand>(1);
        Assert.Single(refunds, refund => refund.Reference == PaymentOperationReferences.Escrow(bookingId));
    }

    [Fact]
    public async Task Cancel_AfterCancellationFailed_RetriesUnderANewOperationId()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptFlatFeeAsync(client);
        var first = await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);
        await first.ShouldBe(HttpStatusCode.NoContent);
        var failedOperationId =
            (await fixture.PaymentTransport.SingleCommandAsync<RefundEscrowCommand>()).OperationId;
        await fixture.RejectLatestFinancialOperationAsync<RefundEscrowCommand>();
        Assert.Equal(BookingState.CancellationFailed, await StateOfAsync(bookingId));

        var retry = await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);

        await retry.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(BookingState.CancellationPending, await StateOfAsync(bookingId));
        var refunds = (await fixture.PaymentTransport.WaitForCommandsAsync<RefundEscrowCommand>(2))
            .Where(command => command.Reference == PaymentOperationReferences.Escrow(bookingId))
            .ToList();
        Assert.Equal(2, refunds.Count);
        Assert.NotEqual(failedOperationId, refunds[1].OperationId);
    }

    #endregion


    [Fact]
    public async Task Cancel_ShouldSucceed_WhenCallerIsArtistPrincipal()
    {
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptFlatFeeAsync(venueClient);
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await artistClient.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);

        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(BookingState.CancellationPending, await StateOfAsync(bookingId));
    }

    [Fact]
    public async Task Cancel_ShouldReturn403_WhenCallerIsNotAPrincipal()
    {
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptFlatFeeAsync(venueClient);
        var unrelatedClient = fixture.CreateClient(fixture.SeedState.VenueManager2);

        var response = await unrelatedClient.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);

        await response.ShouldBe(HttpStatusCode.Forbidden);
        Assert.Equal(BookingState.AwaitingConfirmation, await StateOfAsync(bookingId));
    }

    private async Task<int> AcceptFlatFeeAsync(HttpClient client)
    {
        var applicationId = fixture.SeedState.FlatFeeApp.Id;
        await client.PostAsync($"/api/application/{applicationId}/checkout");
        return await AcceptAsync(client, applicationId, new
        {
            eSignature = new { signatoryName = "Test Signatory" }
        });
    }

    private Task<int> AcceptVenueHireAsync(HttpClient client) =>
        AcceptAsync(client, fixture.SeedState.VenueHireApp.Id, new
        {
            eSignature = new { signatoryName = "Test Signatory" }
        });

    private async Task<int> AcceptDoorSplitAsync(HttpClient client)
    {
        var applicationId = fixture.SeedState.DoorSplitApp.Id;
        await client.PostAsync($"/api/application/{applicationId}/checkout");
        return await AcceptAsync(client, applicationId, new
        {
            eSignature = new { signatoryName = "Test Signatory" }
        });
    }

    private async Task<int> AcceptAsync(
        HttpClient client,
        int applicationId,
        object request)
    {
        var acceptResponse = await client.PostAsync(
            $"/api/application/{applicationId}/accept",
            request);
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        var applicationResponse = await client.GetAsync($"/api/application/{applicationId}");
        await applicationResponse.ShouldBe(HttpStatusCode.OK);
        var application = await applicationResponse.Content.ReadAsync<ApplicationBoundaryResponse>();
        Assert.NotNull(application);
        Assert.Null(application.Actions.Cancel);
        var bookingResponse = await client.GetAsync($"/api/booking/application/{applicationId}");
        await bookingResponse.ShouldBe(HttpStatusCode.OK);
        var booking = await bookingResponse.Content.ReadAsync<BookingSummary>();
        Assert.NotNull(booking);
        return booking.BookingId;
    }

    private async Task<BookingState> StateOfAsync(int bookingId) =>
        (await fixture.Bookings.SingleAsync(value => value.Id == bookingId)).State;

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

    private sealed record ApplicationBoundaryResponse(ApplicationActionsBoundaryResponse Actions);
    private sealed record ApplicationActionsBoundaryResponse(ActionBoundaryResponse? Cancel);
    private sealed record ActionBoundaryResponse(string Href);
}
