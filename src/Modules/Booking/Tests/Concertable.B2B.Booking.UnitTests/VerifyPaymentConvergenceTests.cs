using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Application.Models;
using Concertable.B2B.Booking.Domain.State;
using Concertable.B2B.Booking.Infrastructure.Events;
using Concertable.B2B.Booking.Infrastructure.Services;
using Moq;

namespace Concertable.B2B.Booking.UnitTests;

public sealed class VerifyPaymentConvergenceTests
{
    private readonly Mock<IBookingService> bookings = new(MockBehavior.Strict);

    [Fact]
    public async Task ExecuteAsync_PaymentBeforeAcceptance_AppliesRecordedSuccessAfterCreation()
    {
        var payment = new VerifyPaymentSucceeded(42, "seti_123");
        var accepted = AcceptedApplications.DoorSplit(payment);
        var sequence = new MockSequence();
        this.bookings
            .InSequence(sequence)
            .Setup(service => service.CreateDeferredAsync(
                accepted,
                accepted.PaymentMethodId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeferredBookingDto(7, BookingState.AwaitingConfirmation, "pm_123"));
        this.bookings
            .InSequence(sequence)
            .Setup(service => service.RecordSucceededAsync(
                7,
                It.Is<VerifyPaymentSucceededEvidence>(operation =>
                    operation.ApplicationId == payment.ApplicationId &&
                    operation.Operation == FinancialOperation.VerifyPayment &&
                    operation.ProviderReferenceId == payment.ProviderTransactionId),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var step = new DoorSplitConfirmStep(this.bookings.Object);

        await step.ExecuteAsync(accepted);

        this.bookings.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_AcceptanceBeforePayment_AppliesLaterSuccessToExistingBooking()
    {
        var payment = new VerifyPaymentSucceeded(42, "seti_123");
        this.bookings
            .Setup(service => service.GetByApplicationIdAsync(
                payment.ApplicationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeferredBookingDto(7, BookingState.AwaitingConfirmation, "pm_123"));
        this.bookings
            .Setup(service => service.RecordSucceededAsync(
                7,
                It.Is<VerifyPaymentSucceededEvidence>(operation =>
                    operation.ApplicationId == payment.ApplicationId &&
                    operation.Operation == FinancialOperation.VerifyPayment &&
                    operation.ProviderReferenceId == payment.ProviderTransactionId),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new VerifyPaymentSucceededHandler(this.bookings.Object);

        await handler.HandleAsync(payment);

        this.bookings.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_PaymentFailureBeforeAcceptance_AppliesRequiredFailureAfterCreation()
    {
        var payment = new VerifyPaymentFailed(
            42,
            "seti_123",
            new VerifyPaymentError("card_declined", "Declined"));
        var accepted = AcceptedApplications.DoorSplit(payment);
        this.bookings
            .Setup(service => service.CreateDeferredAsync(
                accepted,
                accepted.PaymentMethodId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeferredBookingDto(7, BookingState.AwaitingConfirmation, "pm_123"));
        this.bookings
            .Setup(service => service.RecordFailedAsync(
                7,
                It.Is<FinancialOperationFailed>(operation =>
                    operation.Error.Code == payment.Error.Code &&
                    operation.Error.Message == payment.Error.Message),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var step = new DoorSplitConfirmStep(this.bookings.Object);

        await step.ExecuteAsync(accepted);

        this.bookings.VerifyAll();
    }
}
