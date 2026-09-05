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
        Assert.Empty(fixture.PaymentTransport.FinancialCommands);
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
        Assert.DoesNotContain(fixture.PaymentTransport.Commands, command => command is RefundEscrowCommand);
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

        // Both refunds carry the booking's one cancellation operation id, so the rejection this covers is a
        // late duplicate of an operation the provider already settled -- not a second pending command.
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
    public async Task Cancel_WhenAnotherCancellationWinsTheRace_QueuesExactlyOneRefund()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptFlatFeeAsync(client);
        var competitor = fixture.CreateClient(fixture.SeedState.VenueManager1);
        fixture.ArmBookingConflict(async () =>
        {
            var winner = await competitor.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);
            await winner.ShouldBe(HttpStatusCode.NoContent);
        });

        var loser = await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);

        await loser.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(1, fixture.Conflicts.ForcedConflicts);
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
    public async Task Cancel_WhenAnotherCancellationWinsTheRace_PublishesOneCancellationEvent()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptDoorSplitAsync(client);
        var competitor = fixture.CreateClient(fixture.SeedState.VenueManager1);
        fixture.ArmBookingConflict(async () =>
        {
            var winner = await competitor.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);
            await winner.ShouldBe(HttpStatusCode.NoContent);
        });

        var loser = await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);

        await loser.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(1, fixture.Conflicts.ForcedConflicts);
        Assert.Equal(BookingState.Cancelled, await StateOfAsync(bookingId));
        Assert.Equal(1, await fixture.GetOutboxMessageCountAsync<BookingCancelledEvent>());
    }

    [Fact]
    public async Task Cancel_WhenConfirmationWinsTheRace_ReturnsConflictAndKeepsTheConfirmedBooking()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptFlatFeeAsync(client);
        var capture = await fixture.PaymentTransport.SingleCommandAsync<CaptureEscrowCommand>();
        fixture.ArmBookingConflict(() => fixture.DispatchIntegrationEventAsync(
            new CaptureEscrowSucceededEvent(capture.OperationId, capture.Reference),
            MessageEnvelope.Create<CaptureEscrowSucceededEvent>(fixture.SeedNow)));

        var cancellation = await client.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);

        await cancellation.ShouldBe(HttpStatusCode.Conflict);
        Assert.Equal(1, fixture.Conflicts.ForcedConflicts);
        var booking = await fixture.Bookings.SingleAsync(value => value.Id == bookingId);
        Assert.Equal(BookingState.Confirmed, booking.State);
        Assert.Null(booking.CancellationOperationId);
        Assert.Equal(1, await fixture.GetConcertCountAsync(bookingId));
        Assert.DoesNotContain(
            fixture.PaymentTransport.Commands,
            command => command is RefundEscrowCommand refund
                && refund.Reference == PaymentOperationReferences.Escrow(bookingId));
    }

    [Fact]
    public async Task Confirmation_WhenCancellationWinsTheRace_RefundsTheCapturedEscrow()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptFlatFeeAsync(client);
        var capture = await fixture.PaymentTransport.SingleCommandAsync<CaptureEscrowCommand>();
        var competitor = fixture.CreateClient(fixture.SeedState.VenueManager1);
        fixture.ArmBookingConflict(async () =>
        {
            var winner = await competitor.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);
            await winner.ShouldBe(HttpStatusCode.NoContent);
        });

        await fixture.DispatchIntegrationEventAsync(
            new CaptureEscrowSucceededEvent(capture.OperationId, capture.Reference),
            MessageEnvelope.Create<CaptureEscrowSucceededEvent>(fixture.SeedNow));

        Assert.Equal(1, fixture.Conflicts.ForcedConflicts);
        Assert.Equal(BookingState.CancellationPending, await StateOfAsync(bookingId));
        Assert.Equal(0, await fixture.GetConcertCountAsync(bookingId));
        Assert.Equal(
            2,
            (await fixture.PaymentTransport.WaitForCommandsAsync<RefundEscrowCommand>(2))
                .Count(command => command.Reference == PaymentOperationReferences.Escrow(bookingId)));
    }

    /// <summary>
    /// A pre-commit handler runs inside the verification's own transaction and does not own it, so a lost race
    /// rolls the verification back and surfaces: convergence is the redelivery, which reads the cancellation
    /// that won and confirms nothing.
    /// </summary>
    [Fact]
    public async Task Cancel_WhenVerifyPaymentConfirmationLosesTheRace_ConvergesOnRedelivery()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var applicationId = fixture.SeedState.DoorSplitApp.Id;
        var bookingId = await AcceptDoorSplitAsync(client);
        var competitor = fixture.CreateClient(fixture.SeedState.VenueManager1);
        fixture.ArmBookingConflict(async () =>
        {
            var winner = await competitor.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);
            await winner.ShouldBe(HttpStatusCode.NoContent);
        });
        var verified = new VerifyPaymentSucceededDomainEvent(
            new VerifyPaymentSucceeded(applicationId));

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => fixture.DispatchPreCommitDomainEventAsync(verified));
        await fixture.DispatchPreCommitDomainEventAsync(verified);

        Assert.Equal(1, fixture.Conflicts.ForcedConflicts);
        Assert.Equal(BookingState.Cancelled, await StateOfAsync(bookingId));
        Assert.Equal(0, await fixture.GetConcertCountAsync(bookingId));
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
    public async Task Cancel_ShouldReturn403_WhenCallerIsArtist()
    {
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var bookingId = await AcceptFlatFeeAsync(venueClient);
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await artistClient.PostAsync($"/api/booking/{bookingId}/cancel", (object?)null);

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

    private sealed record ApplicationBoundaryResponse(ApplicationActionsBoundaryResponse Actions);
    private sealed record ApplicationActionsBoundaryResponse(ActionBoundaryResponse? Cancel);
    private sealed record ActionBoundaryResponse(string Href);
}
