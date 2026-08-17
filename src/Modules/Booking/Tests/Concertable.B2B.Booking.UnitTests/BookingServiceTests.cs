using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Application.Models;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.State;
using Concertable.B2B.Booking.Infrastructure;
using Concertable.B2B.Booking.Infrastructure.Services;
using Moq;

namespace Concertable.B2B.Booking.UnitTests;

public sealed class BookingServiceTests
{
    private readonly Mock<IBookingRepository> bookings = new(MockBehavior.Strict);
    private readonly BookingService service;

    public BookingServiceTests()
    {
        this.service = new BookingService(
            this.bookings.Object,
            Mock.Of<IContractRepository>(),
            Mock.Of<IUnitOfWorkBehavior>(),
            TimeProvider.System);
    }

    [Fact]
    public void Create_AcceptedApplication_CopiesProvenanceAndExpectedOperation()
    {
        var accepted = AcceptedApplications.DoorSplit();

        var booking = DeferredBooking.Create(accepted, accepted.PaymentMethodId);

        Assert.Equal(accepted.OperationId, booking.OperationId);
        Assert.Equal(accepted.ApplicationId, booking.ApplicationId);
        Assert.Equal(FinancialOperation.VerifyPayment, booking.ExpectedFinancialOperation);
    }

    [Fact]
    public void Create_MissingAcceptedApplication_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => DeferredBooking.Create(null!, "pm_123"));

    [Fact]
    public async Task RecordSucceededAsync_MismatchedApplication_RejectsWithoutMutation()
    {
        var booking = DeferredBooking.Create(AcceptedApplications.DoorSplit(), "pm_123");
        this.bookings
            .Setup(repository => repository.GetByIdAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        var operation = new FinancialOperationSucceeded(
            99,
            FinancialOperation.VerifyPayment,
            "seti_123");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => this.service.RecordSucceededAsync(0, operation));

        Assert.Equal(BookingState.AwaitingFinancialConfirmation, booking.State);
        this.bookings.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RecordSucceededAsync_MismatchedOperation_RejectsWithoutMutation()
    {
        var booking = DeferredBooking.Create(AcceptedApplications.DoorSplit(), "pm_123");
        this.bookings
            .Setup(repository => repository.GetByIdAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        var operation = new FinancialOperationSucceeded(
            booking.ApplicationId,
            FinancialOperation.CaptureEscrow,
            "pi_123");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => this.service.RecordSucceededAsync(0, operation));

        Assert.Equal(BookingState.AwaitingFinancialConfirmation, booking.State);
        this.bookings.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RecordSucceededAsync_DuplicateCallback_ConfirmsExactlyOnce()
    {
        var booking = DeferredBooking.Create(AcceptedApplications.DoorSplit(), "pm_123");
        this.bookings
            .Setup(repository => repository.GetByIdAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        this.bookings
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var operation = new FinancialOperationSucceeded(
            booking.ApplicationId,
            FinancialOperation.VerifyPayment,
            "seti_123");

        await this.service.RecordSucceededAsync(0, operation);
        booking.ClearDomainEvents();
        await this.service.RecordSucceededAsync(0, operation);

        Assert.Equal(BookingState.Confirmed, booking.State);
        Assert.Equal(operation.ProviderReferenceId, booking.FinancialOperationReferenceId);
        Assert.Empty(booking.DomainEvents);
        this.bookings.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordFailedAsync_RequiredError_RecordsFailureFact()
    {
        var booking = DeferredBooking.Create(AcceptedApplications.DoorSplit(), "pm_123");
        this.bookings
            .Setup(repository => repository.GetByIdAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        this.bookings
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var operation = new FinancialOperationFailed(
            booking.ApplicationId,
            FinancialOperation.VerifyPayment,
            "seti_123",
            new FinancialOperationError("card_declined", "Declined"));

        await this.service.RecordFailedAsync(0, operation);

        Assert.Equal(BookingState.FinancialConfirmationFailed, booking.State);
        Assert.Equal("seti_123", booking.FinancialOperationReferenceId);
        Assert.Equal("card_declined", booking.FinancialFailureCode);
        Assert.Equal("Declined", booking.FinancialFailureMessage);
    }
}
