using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Booking.Application.Mappers;
using Concertable.B2B.Booking.Application.Models;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.State;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class BookingService : IBookingService
{
    private readonly IBookingRepository bookings;
    private readonly IContractRepository contracts;
    private readonly IUnitOfWorkBehavior unitOfWork;
    private readonly TimeProvider timeProvider;

    public BookingService(
        IBookingRepository bookings,
        IContractRepository contracts,
        IUnitOfWorkBehavior unitOfWork,
        TimeProvider timeProvider)
    {
        this.bookings = bookings;
        this.contracts = contracts;
        this.unitOfWork = unitOfWork;
        this.timeProvider = timeProvider;
    }

    public Task<StandardBookingDto> CreateStandardAsync(
        AcceptedApplication application,
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(async () =>
        {
            var booking = StandardBooking.Create(application);
            await PersistAsync(booking, application, ct);
            return (StandardBookingDto)booking.ToDto();
        }, ct);

    public Task<DeferredBookingDto> CreateDeferredAsync(
        AcceptedApplication application,
        string paymentMethodId,
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(async () =>
        {
            var booking = DeferredBooking.Create(application, paymentMethodId);
            await PersistAsync(booking, application, ct);
            return (DeferredBookingDto)booking.ToDto();
        }, ct);

    public async Task<BookingDto?> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        (await bookings.GetByApplicationIdAsync(applicationId, ct))?.ToDto();

    public async Task RecordSucceededAsync(
        int bookingId,
        FinancialOperationSucceeded operation,
        CancellationToken ct = default)
    {
        var booking = await bookings.GetByIdAsync(bookingId, ct)
            ?? throw new InvalidOperationException($"Booking {bookingId} was not found during confirmation.");
        Validate(booking, operation);

        if (booking.State == BookingState.Confirmed)
        {
            EnsureSameProviderReference(booking, operation);
            return;
        }

        booking.RecordFinancialConfirmation(operation.ProviderReferenceId);
        await bookings.SaveChangesAsync(ct);
    }

    public async Task RecordFailedAsync(
        int bookingId,
        FinancialOperationFailed operation,
        CancellationToken ct = default)
    {
        var booking = await bookings.GetByIdAsync(bookingId, ct)
            ?? throw new InvalidOperationException($"Booking {bookingId} was not found during confirmation.");
        Validate(booking, operation);

        if (booking.State == BookingState.Confirmed)
        {
            EnsureSameProviderReference(booking, operation);
            return;
        }
        if (booking.State == BookingState.FinancialConfirmationFailed &&
            booking.FinancialOperationReferenceId == operation.ProviderReferenceId)
            return;

        booking.RecordFinancialFailure(
            operation.ProviderReferenceId,
            operation.Error.Code,
            operation.Error.Message);
        await bookings.SaveChangesAsync(ct);
    }

    private static void Validate(BookingEntity booking, FinancialOperationEvidence operation)
    {
        if (booking.ApplicationId != operation.ApplicationId)
            throw new InvalidOperationException(
                $"Financial operation for application {operation.ApplicationId} cannot advance booking {booking.Id} for application {booking.ApplicationId}.");
        if (booking.ExpectedFinancialOperation != operation.Operation)
            throw new InvalidOperationException(
                $"Booking {booking.Id} expects {booking.ExpectedFinancialOperation}, not {operation.Operation}.");
    }

    private static void EnsureSameProviderReference(
        BookingEntity booking,
        FinancialOperationEvidence operation)
    {
        if (booking.FinancialOperationReferenceId != operation.ProviderReferenceId)
            throw new InvalidOperationException(
                $"Booking {booking.Id} was confirmed by provider reference {booking.FinancialOperationReferenceId}, not {operation.ProviderReferenceId}.");
    }

    private async Task PersistAsync(
        BookingEntity booking,
        AcceptedApplication application,
        CancellationToken ct)
    {
        await bookings.AddAsync(booking, ct);
        await bookings.SaveChangesAsync(ct);

        var contract = ContractEntity.Create(
            booking.Id,
            application,
            timeProvider.GetUtcNow().UtcDateTime);
        await contracts.AddAsync(contract, ct);
        await contracts.SaveChangesAsync(ct);
    }
}
