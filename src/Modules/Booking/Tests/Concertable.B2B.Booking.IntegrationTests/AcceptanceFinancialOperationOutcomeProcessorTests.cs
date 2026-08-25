using Concertable.B2B.Booking.Domain.State;
using Concertable.B2B.Booking.Infrastructure.Events;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Concertable.B2B.Booking.IntegrationTests;

[Collection("Integration")]
public sealed class AcceptanceFinancialOperationOutcomeProcessorTests : IAsyncLifetime
{
    private readonly BookingApiFixture fixture;

    public AcceptanceFinancialOperationOutcomeProcessorTests(BookingApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task DuplicateCaptureSuccess_IsAcknowledgedAndAppliedExactlyOnce()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var applicationId = fixture.SeedState.FlatFeeApp.Id;
        await client.PostAsync($"/api/application/{applicationId}/checkout");
        var accept = await client.PostAsync(
            $"/api/application/{applicationId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await accept.ShouldBe(System.Net.HttpStatusCode.NoContent);
        var command = Assert.Single(
            await fixture.PaymentTransport.WaitForCommandsAsync<CaptureEscrowCommand>(1));
        var envelope = new MessageEnvelope(
            Guid.NewGuid(),
            MessageTypeAttribute.Resolve(typeof(CaptureEscrowSucceededEvent)),
            DateTimeOffset.UtcNow);
        var succeeded = new CaptureEscrowSucceededEvent(
            command.OperationId,
            command.BookingId,
            "pi_capture_123");

        await fixture.DispatchIntegrationEventAsync(succeeded, envelope);
        await fixture.DispatchIntegrationEventAsync(succeeded, envelope);

        var booking = await fixture.Bookings.SingleAsync(value => value.Id == command.BookingId);
        Assert.Equal(BookingState.Confirmed, booking.State);
        Assert.Equal("pi_capture_123", booking.FinancialOperationReferenceId);
        var inbox = await fixture.InboxMessages
            .Where(message =>
                message.MessageId == envelope.MessageId &&
                message.ConsumerName == nameof(AcceptanceFinancialOperationOutcomeProcessor))
            .ToListAsync();
        Assert.Single(inbox);
    }

    [Fact]
    public async Task CaptureSuccess_WhenBookingSaveFails_RollsBackBookingConcertAndInbox()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var applicationId = fixture.SeedState.FlatFeeApp.Id;
        await client.PostAsync($"/api/application/{applicationId}/checkout");
        var accept = await client.PostAsync(
            $"/api/application/{applicationId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await accept.ShouldBe(System.Net.HttpStatusCode.NoContent);
        var command = Assert.Single(
            await fixture.PaymentTransport.WaitForCommandsAsync<CaptureEscrowCommand>(1));
        var envelope = new MessageEnvelope(
            Guid.NewGuid(),
            MessageTypeAttribute.Resolve(typeof(CaptureEscrowSucceededEvent)),
            DateTimeOffset.UtcNow);
        var succeeded = new CaptureEscrowSucceededEvent(
            command.OperationId,
            command.BookingId,
            "pi_capture_rollback");

        await fixture.FailBookingUpdatesAsync();
        try
        {
            await Assert.ThrowsAsync<DbUpdateException>(
                () => fixture.DispatchIntegrationEventAsync(succeeded, envelope));
        }
        finally
        {
            await fixture.RestoreBookingUpdatesAsync();
        }

        var booking = await fixture.Bookings.SingleAsync(value => value.Id == command.BookingId);
        Assert.Equal(BookingState.AwaitingConfirmation, booking.State);
        Assert.Null(booking.FinancialOperationReferenceId);
        Assert.Equal(0, await fixture.GetConcertCountAsync(command.BookingId));
        var inbox = await fixture.InboxMessages
            .Where(message =>
                message.MessageId == envelope.MessageId &&
                message.ConsumerName == nameof(AcceptanceFinancialOperationOutcomeProcessor))
            .ToListAsync();
        Assert.Empty(inbox);
    }
}
