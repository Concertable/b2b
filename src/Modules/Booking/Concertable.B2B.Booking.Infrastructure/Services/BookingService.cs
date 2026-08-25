using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Booking.Application.Errors;
using Concertable.B2B.Booking.Application.Mappers;
using Concertable.B2B.Booking.Application.Models;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class BookingService : IBookingService
{
    private readonly IBookingRepository bookings;
    private readonly IContractRepository contracts;
    private readonly IUnitOfWorkBehavior unitOfWork;
    private readonly IBus bus;
    private readonly IOutboxUnitOfWorkBehavior outbox;
    private readonly IBookingCancellationExecutor cancellation;
    private readonly TimeProvider timeProvider;

    public BookingService(
        IBookingRepository bookings,
        IContractRepository contracts,
        IUnitOfWorkBehavior unitOfWork,
        IBus bus,
        IOutboxUnitOfWorkBehavior outbox,
        IBookingCancellationExecutor cancellation,
        TimeProvider timeProvider)
    {
        this.bookings = bookings;
        this.contracts = contracts;
        this.unitOfWork = unitOfWork;
        this.bus = bus;
        this.outbox = outbox;
        this.cancellation = cancellation;
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

    public Task<int?> GetIdByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        bookings.GetIdByApplicationIdAsync(applicationId, ct);

    public async Task<BookingSummaryDto?> GetSummaryByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        var booking = await bookings.GetByApplicationIdAsync(applicationId, ct);
        return booking is null
            ? null
            : new BookingSummaryDto(
                booking.Id,
                booking.ApplicationId,
                booking.State,
                booking.OperationId,
                booking.FinancialFailureCode,
                booking.FinancialFailureMessage);
    }

    public async Task<IReadOnlyList<BookingSummaryDto>> GetSummariesByApplicationIdsAsync(
        IReadOnlyCollection<int> applicationIds,
        CancellationToken ct = default) =>
        (await bookings.GetByApplicationIdsAsync(applicationIds, ct))
            .Select(booking => new BookingSummaryDto(
                booking.Id,
                booking.ApplicationId,
                booking.State,
                booking.OperationId,
                booking.FinancialFailureCode,
                booking.FinancialFailureMessage))
            .ToList();

    public Task<int> GetArtistAwaitingCheckoutCountAsync(
        Guid artistTenantId,
        CancellationToken ct = default) =>
        bookings.GetAwaitingCheckoutCountByArtistTenantIdAsync(
            artistTenantId,
            timeProvider.GetUtcNow().UtcDateTime,
            ct);

    public async Task<UnitResult<CancelBookingError>> CancelByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        var booking = await bookings.GetByApplicationIdAsync(applicationId, ct);
        return booking is null
            ? new CancelBookingError.BookingNotFound(applicationId)
            : await CancelAsync(booking.Id, ct);
    }

    public Task<UnitResult<CancelBookingError>> CancelAsync(
        int bookingId,
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(
            () => outbox.ExecuteAsync(async () =>
            {
                var booking = await bookings.GetForUpdateByIdAsync(bookingId, ct);
                if (booking is null)
                    return (UnitResult<CancelBookingError>)new CancelBookingError.BookingNotFound(bookingId);
                if (booking.State is State.Cancelled or State.CancellationPending)
                    return UnitResult.Success<CancelBookingError>();
                if (booking.ValidateBeginCancellation().TryGetError(out var transitionError))
                    return new CancelBookingError.InvalidTransition(transitionError);

                await this.cancellation.ExecuteAsync(booking, ct);
                await bookings.SaveChangesAsync(ct);
                return UnitResult.Success<CancelBookingError>();
            }, ct),
            ct);

    public Task RecordSucceededAsync(
        int bookingId,
        FinancialOperationSucceeded operation,
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(() => RecordSucceededCoreAsync(bookingId, operation, ct), ct);

    public Task RecordFailedAsync(
        int bookingId,
        FinancialOperationFailed operation,
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(() => RecordFailedCoreAsync(bookingId, operation, ct), ct);

    private async Task RecordSucceededCoreAsync(
        int bookingId,
        FinancialOperationSucceeded operation,
        CancellationToken ct)
    {
        var booking = await bookings.GetForUpdateByIdAsync(bookingId, ct)
            ?? throw new InvalidOperationException($"Booking {bookingId} was not found during confirmation.");
        Validate(bookingId, booking, operation);

        if (booking.State == State.CancellationPending)
        {
            await bus.SendAsync(new RefundEscrowCommand(
                booking.CancellationOperationId!.Value,
                bookingId,
                RefundReasonCodes.RequestedByCustomer), ct);
            return;
        }
        if (booking.State is State.CancellationFailed or State.Cancelled)
            return;
        if (booking.State == State.Confirmed)
        {
            EnsureSameProviderReference(booking, operation);
            return;
        }

        if (booking.RecordFinancialConfirmation(operation.ProviderReferenceId).TryGetError(out var transitionError))
            throw new InvalidOperationException($"Booking cannot confirm from {transitionError.Current}.");
        await bookings.SaveChangesAsync(ct);
    }

    private async Task RecordFailedCoreAsync(
        int bookingId,
        FinancialOperationFailed operation,
        CancellationToken ct)
    {
        var booking = await bookings.GetForUpdateByIdAsync(bookingId, ct)
            ?? throw new InvalidOperationException($"Booking {bookingId} was not found during confirmation.");
        Validate(bookingId, booking, operation);

        if (booking.State == State.Confirmed)
            return;
        if (booking.State == State.CancellationPending)
        {
            if (booking.Cancel().TryGetError(out var transitionError))
                throw new InvalidOperationException($"Booking cannot cancel from {transitionError.Current}.");
            await bookings.SaveChangesAsync(ct);
            return;
        }
        if (IsDuplicateFailure(booking, operation))
            return;

        switch (operation)
        {
            case VerifyPaymentFailedEvidence verified:
                if (booking.RecordFinancialFailure(
                    verified.ProviderReferenceId,
                    verified.Error.Code,
                    verified.Error.Message).TryGetError(out var failureError))
                    throw new InvalidOperationException($"Booking cannot record confirmation failure from {failureError.Current}.");
                break;
            case AcceptanceFinancialOperationRejected rejected:
                if (booking.RecordFinancialRejection(rejected.Error.Code, rejected.Error.Message).TryGetError(out var rejectionError))
                    throw new InvalidOperationException($"Booking cannot record confirmation rejection from {rejectionError.Current}.");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
        }
        await bookings.SaveChangesAsync(ct);
    }

    private static void Validate(
        int bookingId,
        BookingEntity booking,
        FinancialOperationEvidence operation)
    {
        if (booking.ExpectedFinancialOperation != operation.Operation)
            throw new InvalidOperationException(
                $"Booking {booking.Id} expects {booking.ExpectedFinancialOperation}, not {operation.Operation}.");

        switch (operation)
        {
            case VerifyPaymentSucceededEvidence verified
                when booking.ApplicationId != verified.ApplicationId:
                throw new InvalidOperationException(
                    $"Financial operation for application {verified.ApplicationId} cannot advance booking {booking.Id} for application {booking.ApplicationId}.");
            case VerifyPaymentFailedEvidence failed
                when booking.ApplicationId != failed.ApplicationId:
                throw new InvalidOperationException(
                    $"Financial operation for application {failed.ApplicationId} cannot advance booking {booking.Id} for application {booking.ApplicationId}.");
            case AcceptanceFinancialOperationSucceeded accepted
                when bookingId != accepted.BookingId || booking.OperationId != accepted.OperationId:
                throw new InvalidOperationException(
                    $"Acceptance operation {accepted.OperationId} for booking {accepted.BookingId} cannot advance booking {booking.Id} with operation {booking.OperationId}.");
            case AcceptanceFinancialOperationRejected accepted
                when bookingId != accepted.BookingId || booking.OperationId != accepted.OperationId:
                throw new InvalidOperationException(
                    $"Acceptance operation {accepted.OperationId} for booking {accepted.BookingId} cannot advance booking {booking.Id} with operation {booking.OperationId}.");
        }
    }

    private static bool IsDuplicateFailure(
        BookingEntity booking,
        FinancialOperationFailed operation) =>
        booking.State == State.ConfirmationFailed && operation switch
        {
            VerifyPaymentFailedEvidence verified =>
                booking.FinancialOperationReferenceId == verified.ProviderReferenceId,
            AcceptanceFinancialOperationRejected rejected =>
                booking.FinancialOperationReferenceId is null &&
                booking.FinancialFailureCode == rejected.Error.Code &&
                booking.FinancialFailureMessage == rejected.Error.Message,
            _ => false
        };

    private static void EnsureSameProviderReference(
        BookingEntity booking,
        FinancialOperationSucceeded operation)
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
