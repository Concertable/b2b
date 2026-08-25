using System.Net;
using Concertable.B2B.Booking.Domain.State;
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
        var booking = await AcceptFlatFeeAsync(client);

        var response = await client.PostAsync(booking.CancelHref, (object?)null);

        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(BookingState.CancellationPending, await StateOfAsync(booking.Id));
        var refund = fixture.PaymentTransport.SingleCommand<RefundEscrowCommand>();
        Assert.Equal(booking.Id, refund.BookingId);
        Assert.Equal(RefundReasonCodes.RequestedByCustomer, refund.Reason);
        await fixture.CompleteLatestFinancialOperationAsync<RefundEscrowCommand>();
        Assert.Equal(BookingState.Cancelled, await StateOfAsync(booking.Id));
    }

    [Fact]
    public async Task Cancel_ShouldMarkCancelled_WithoutHeldEscrow()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var booking = await AcceptDoorSplitAsync(client);

        var response = await client.PostAsync(booking.CancelHref, (object?)null);

        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.DoesNotContain(fixture.PaymentTransport.Commands, command => command is RefundEscrowCommand);
        Assert.Equal(BookingState.Cancelled, await StateOfAsync(booking.Id));
        Assert.Empty(fixture.EscrowClient.Holds);
    }

    [Fact]
    public async Task Cancel_ShouldMarkCancelled_FromConfirmationFailed()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var booking = await AcceptVenueHireAsync(client);
        await fixture.SendEscrowFailedWebhookAsync(booking.Id);
        Assert.Equal(BookingState.ConfirmationFailed, await StateOfAsync(booking.Id));

        var response = await client.PostAsync(booking.CancelHref, (object?)null);

        await response.ShouldBe(HttpStatusCode.NoContent);
        Assert.DoesNotContain(fixture.PaymentTransport.Commands, command => command is RefundEscrowCommand);
        Assert.Equal(BookingState.Cancelled, await StateOfAsync(booking.Id));
    }

    [Fact]
    public async Task Cancel_ShouldComplete_WhenEscrowRejectionLandsAfterCancellation()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var booking = await AcceptFlatFeeAsync(client);
        var cancelResponse = await client.PostAsync(booking.CancelHref, (object?)null);
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(BookingState.CancellationPending, await StateOfAsync(booking.Id));

        await fixture.SendEscrowFailedWebhookAsync(booking.Id);

        Assert.Equal(BookingState.Cancelled, await StateOfAsync(booking.Id));
    }

    [Fact]
    public async Task Cancel_ShouldRefundAgainAndStayCancelled_WhenEscrowCaptureLandsAfterCancel()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var booking = await AcceptVenueHireAsync(client);
        var cancelResponse = await client.PostAsync(booking.CancelHref, (object?)null);
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);

        await fixture.StripeClient.SendWebhookAsync();
        var refunds = await fixture.PaymentTransport.WaitForCommandsAsync<RefundEscrowCommand>(2);
        await fixture.CompleteLatestFinancialOperationAsync<RefundEscrowCommand>();

        Assert.Equal(BookingState.Cancelled, await StateOfAsync(booking.Id));
        Assert.Equal(2, refunds.Count(command => command.BookingId == booking.Id));
    }

    [Fact]
    public async Task Cancel_ShouldRecordCancellationFailure_WhenRefundIsRejected()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var booking = await AcceptFlatFeeAsync(client);
        var cancelResponse = await client.PostAsync(booking.CancelHref, (object?)null);
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);

        await fixture.RejectLatestFinancialOperationAsync();

        var entity = await fixture.Bookings.SingleAsync(value => value.Id == booking.Id);
        Assert.Equal(BookingState.CancellationFailed, entity.State);
        Assert.Equal("refund_failed", entity.FinancialFailureCode);
        Assert.Equal("Refund failed", entity.FinancialFailureMessage);
    }

    [Fact]
    public async Task Cancel_ShouldReturn409_WhenConfirmed()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var booking = await AcceptFlatFeeAsync(client);
        await fixture.StripeClient.SendWebhookAsync();
        Assert.Equal(BookingState.Confirmed, await StateOfAsync(booking.Id));

        var response = await client.PostAsync(booking.CancelHref, (object?)null);

        await response.ShouldBe(HttpStatusCode.Conflict);
        Assert.Equal(BookingState.Confirmed, await StateOfAsync(booking.Id));
    }

    [Fact]
    public async Task Cancel_WhenQueuedBeforeConfirmation_ConvergesOnCancellation()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var booking = await AcceptFlatFeeAsync(client);
        var capture = fixture.PaymentTransport.SingleCommand<CaptureEscrowCommand>();
        await using var bookingLock = await fixture.HoldBookingForUpdateAsync(booking.Id);
        var cancellationTask = client.PostAsync(booking.CancelHref, (object?)null);
        await fixture.WaitForBookingLockWaitersAsync(1);
        var confirmationTask = fixture.DispatchIntegrationEventAsync(
            new CaptureEscrowSucceededEvent(capture.OperationId, booking.Id, "pi_cancel_first"),
            MessageEnvelope.Create<CaptureEscrowSucceededEvent>(DateTimeOffset.UtcNow));
        await fixture.WaitForBookingLockWaitersAsync(2);

        await bookingLock.RollbackAsync();
        var cancellation = await cancellationTask;
        await confirmationTask;

        await cancellation.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(BookingState.CancellationPending, await StateOfAsync(booking.Id));
        Assert.Equal(0, await fixture.GetConcertCountAsync(booking.Id));
        Assert.Equal(
            2,
            (await fixture.PaymentTransport.WaitForCommandsAsync<RefundEscrowCommand>(2))
                .Count(command => command.BookingId == booking.Id));
    }

    [Fact]
    public async Task Cancel_WhenQueuedAfterConfirmation_LeavesConfirmedBooking()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var booking = await AcceptFlatFeeAsync(client);
        var capture = fixture.PaymentTransport.SingleCommand<CaptureEscrowCommand>();
        await using var bookingLock = await fixture.HoldBookingForUpdateAsync(booking.Id);
        var confirmationTask = fixture.DispatchIntegrationEventAsync(
            new CaptureEscrowSucceededEvent(capture.OperationId, booking.Id, "pi_confirm_first"),
            MessageEnvelope.Create<CaptureEscrowSucceededEvent>(DateTimeOffset.UtcNow));
        await fixture.WaitForBookingLockWaitersAsync(1);
        var cancellationTask = client.PostAsync(booking.CancelHref, (object?)null);
        await fixture.WaitForBookingLockWaitersAsync(2);

        await bookingLock.RollbackAsync();
        await confirmationTask;
        var cancellation = await cancellationTask;

        await cancellation.ShouldBe(HttpStatusCode.Conflict);
        var entity = await fixture.Bookings.SingleAsync(value => value.Id == booking.Id);
        Assert.Equal(BookingState.Confirmed, entity.State);
        Assert.Equal("pi_confirm_first", entity.FinancialOperationReferenceId);
        Assert.Equal(1, await fixture.GetConcertCountAsync(booking.Id));
        Assert.DoesNotContain(
            fixture.PaymentTransport.Commands,
            command => command is RefundEscrowCommand refund && refund.BookingId == booking.Id);
    }

    [Fact]
    public async Task Cancel_ShouldReturn403_WhenCallerIsArtist()
    {
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var booking = await AcceptFlatFeeAsync(venueClient);
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await artistClient.PostAsync(booking.CancelHref, (object?)null);

        await response.ShouldBe(HttpStatusCode.Forbidden);
        Assert.Equal(BookingState.AwaitingConfirmation, await StateOfAsync(booking.Id));
    }

    private async Task<BookingBoundary> AcceptFlatFeeAsync(HttpClient client)
    {
        var applicationId = fixture.SeedState.FlatFeeApp.Id;
        await client.PostAsync($"/api/application/{applicationId}/checkout");
        return await AcceptAsync(client, applicationId, new
        {
            eSignature = new { signatoryName = "Test Signatory" }
        });
    }

    private Task<BookingBoundary> AcceptVenueHireAsync(HttpClient client) =>
        AcceptAsync(client, fixture.SeedState.VenueHireApp.Id, new
        {
            eSignature = new { signatoryName = "Test Signatory" }
        });

    private async Task<BookingBoundary> AcceptDoorSplitAsync(HttpClient client)
    {
        var applicationId = fixture.SeedState.DoorSplitApp.Id;
        await client.PostAsync($"/api/application/{applicationId}/checkout");
        return await AcceptAsync(client, applicationId, new
        {
            eSignature = new { signatoryName = "Test Signatory" },
            paymentMethodId = "pm_card_visa"
        });
    }

    private static async Task<BookingBoundary> AcceptAsync(
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
        Assert.NotNull(application.Actions.Cancel);
        var cancelHref = application.Actions.Cancel.Href;
        return new BookingBoundary(int.Parse(cancelHref.Split('/')[3]), cancelHref);
    }

    private async Task<BookingState> StateOfAsync(int bookingId) =>
        (await fixture.Bookings.SingleAsync(value => value.Id == bookingId)).State;

    private sealed record ApplicationBoundaryResponse(ApplicationActionsBoundaryResponse Actions);
    private sealed record ApplicationActionsBoundaryResponse(ActionBoundaryResponse? Cancel);
    private sealed record ActionBoundaryResponse(string Href);
    private sealed record BookingBoundary(int Id, string CancelHref);
}
