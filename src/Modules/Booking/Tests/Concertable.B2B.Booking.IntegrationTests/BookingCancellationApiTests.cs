using System.Net;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Contracts;
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
        Assert.Equal(State.CancellationPending, await StateOfAsync(bookingId));
        var refund = Assert.Single(
            await fixture.PaymentTransport.WaitForCommandsAsync<RefundEscrowCommand>(1));
        Assert.Equal(bookingId, refund.BookingId);
        Assert.Equal(RefundReasonCodes.RequestedByCustomer, refund.Reason);
        await fixture.CompleteLatestFinancialOperationAsync<RefundEscrowCommand>();
        Assert.Equal(State.Cancelled, await StateOfAsync(bookingId));
    }

    [Fact]
    public async Task Cancel_ShouldMarkCancelled_WithoutHeldEscrow()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptDoorSplitAsync(client);

        var response = await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);

        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.DoesNotContain(fixture.PaymentTransport.Commands, command => command is RefundEscrowCommand);
        Assert.Equal(State.Cancelled, await StateOfAsync(bookingId));
        Assert.Empty(fixture.EscrowClient.Holds);
    }

    [Fact]
    public async Task Cancel_ShouldMarkCancelled_FromConfirmationFailed()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptVenueHireAsync(client);
        await fixture.SendEscrowFailedWebhookAsync(bookingId);
        Assert.Equal(State.ConfirmationFailed, await StateOfAsync(bookingId));

        var response = await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);

        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.DoesNotContain(fixture.PaymentTransport.Commands, command => command is RefundEscrowCommand);
        Assert.Equal(State.Cancelled, await StateOfAsync(bookingId));
    }

    [Fact]
    public async Task Cancel_ShouldComplete_WhenEscrowRejectionLandsAfterCancellation()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptFlatFeeAsync(client);
        var cancelResponse = await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(State.CancellationPending, await StateOfAsync(bookingId));

        await fixture.SendEscrowFailedWebhookAsync(bookingId);

        Assert.Equal(State.Cancelled, await StateOfAsync(bookingId));
    }

    [Fact]
    public async Task Cancel_ShouldRefundAgainAndStayCancelled_WhenEscrowCaptureLandsAfterCancel()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptVenueHireAsync(client);
        var cancelResponse = await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);

        await fixture.StripeClient.SendWebhookAsync();
        var refunds = await fixture.PaymentTransport.WaitForCommandsAsync<RefundEscrowCommand>(2);
        await fixture.CompleteLatestFinancialOperationAsync<RefundEscrowCommand>();

        Assert.Equal(State.Cancelled, await StateOfAsync(bookingId));
        Assert.Equal(2, refunds.Count(command => command.BookingId == bookingId));
    }

    [Fact]
    public async Task Cancel_ShouldRecordCancellationFailure_WhenRefundIsRejected()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptFlatFeeAsync(client);
        var cancelResponse = await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);

        await fixture.RejectLatestFinancialOperationAsync();

        var entity = await fixture.Bookings.SingleAsync(value => value.Id == bookingId);
        Assert.Equal(State.CancellationFailed, entity.State);
        Assert.Equal("refund_failed", entity.FinancialFailureCode);
        Assert.Equal("Refund failed", entity.FinancialFailureMessage);
    }

    [Fact]
    public async Task Cancel_ShouldReturn409_WhenConfirmed()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptFlatFeeAsync(client);
        await fixture.StripeClient.SendWebhookAsync();
        Assert.Equal(State.Confirmed, await StateOfAsync(bookingId));

        var response = await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);

        await response.ShouldBe(HttpStatusCode.Conflict);
        Assert.Equal(State.Confirmed, await StateOfAsync(bookingId));
    }

    [Fact]
    public async Task Cancel_WhenQueuedBeforeConfirmation_ConvergesOnCancellation()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptFlatFeeAsync(client);
        var capture = fixture.PaymentTransport.SingleCommand<CaptureEscrowCommand>();
        await using var bookingLock = await fixture.HoldBookingForUpdateAsync(bookingId);
        var cancellationTask = client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);
        await fixture.WaitForBookingLockWaitersAsync(1);
        var confirmationTask = fixture.DispatchIntegrationEventAsync(
            new CaptureEscrowSucceededEvent(capture.OperationId, bookingId, "pi_cancel_first"),
            MessageEnvelope.Create<CaptureEscrowSucceededEvent>(DateTimeOffset.UtcNow));
        await fixture.WaitForBookingLockWaitersAsync(2);

        await bookingLock.RollbackAsync();
        var cancellation = await cancellationTask;
        await confirmationTask;

        await cancellation.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(State.CancellationPending, await StateOfAsync(bookingId));
        Assert.Equal(0, await fixture.GetConcertCountAsync(bookingId));
        Assert.Equal(
            2,
            (await fixture.PaymentTransport.WaitForCommandsAsync<RefundEscrowCommand>(2))
                .Count(command => command.BookingId == bookingId));
    }

    [Fact]
    public async Task Cancel_WhenQueuedBeforeVerifyPaymentConfirmation_ConvergesOnCancellation()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var applicationId = fixture.SeedState.DoorSplitApp.Id;
        var bookingId = await AcceptDoorSplitAsync(client);
        await using var bookingLock = await fixture.HoldBookingForUpdateAsync(bookingId);
        var cancellationTask = client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);
        await fixture.WaitForBookingLockWaitersAsync(1);
        var confirmationTask = fixture.DispatchPreCommitDomainEventAsync(
            new VerifyPaymentSucceeded(applicationId, "seti_cancel_first"));
        await fixture.WaitForBookingLockWaitersAsync(2);

        await bookingLock.RollbackAsync();
        var cancellation = await cancellationTask;
        await confirmationTask;

        await cancellation.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(State.Cancelled, await StateOfAsync(bookingId));
        Assert.Equal(0, await fixture.GetConcertCountAsync(bookingId));
    }

    [Fact]
    public async Task Cancel_WhenQueuedAfterConfirmation_LeavesConfirmedBooking()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptFlatFeeAsync(client);
        var capture = fixture.PaymentTransport.SingleCommand<CaptureEscrowCommand>();
        await using var bookingLock = await fixture.HoldBookingForUpdateAsync(bookingId);
        var confirmationTask = fixture.DispatchIntegrationEventAsync(
            new CaptureEscrowSucceededEvent(capture.OperationId, bookingId, "pi_confirm_first"),
            MessageEnvelope.Create<CaptureEscrowSucceededEvent>(DateTimeOffset.UtcNow));
        await fixture.WaitForBookingLockWaitersAsync(1);
        var cancellationTask = client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);
        await fixture.WaitForBookingLockWaitersAsync(2);

        await bookingLock.RollbackAsync();
        await confirmationTask;
        var cancellation = await cancellationTask;

        await cancellation.ShouldBe(HttpStatusCode.Conflict);
        var entity = await fixture.Bookings.SingleAsync(value => value.Id == bookingId);
        Assert.Equal(State.Confirmed, entity.State);
        Assert.Equal("pi_confirm_first", entity.FinancialOperationReferenceId);
        Assert.Equal(1, await fixture.GetConcertCountAsync(bookingId));
        Assert.DoesNotContain(
            fixture.PaymentTransport.Commands,
            command => command is RefundEscrowCommand refund && refund.BookingId == bookingId);
    }

    [Fact]
    public async Task Cancel_ShouldReturn403_WhenCallerIsArtist()
    {
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptFlatFeeAsync(venueClient);
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await artistClient.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);

        await response.ShouldBe(HttpStatusCode.Forbidden);
        Assert.Equal(State.AwaitingConfirmation, await StateOfAsync(bookingId));
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
            eSignature = new { signatoryName = "Test Signatory" },
            paymentMethodId = "pm_card_visa"
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

    private async Task<State> StateOfAsync(int bookingId) =>
        (await fixture.Bookings.SingleAsync(value => value.Id == bookingId)).State;

    private sealed record ApplicationBoundaryResponse(ApplicationActionsBoundaryResponse Actions);
    private sealed record ApplicationActionsBoundaryResponse(ActionBoundaryResponse? Cancel);
    private sealed record ActionBoundaryResponse(string Href);
}
