using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Booking.Application.Errors;
using Concertable.B2B.Booking.Application.Mappers;
using Concertable.B2B.Booking.Application.Models;
using Concertable.B2B.Booking.Application.Strategies;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Factories;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Infrastructure.Extensions;
using Concertable.B2B.Infrastructure.Payments;
using Concertable.B2B.Booking.Infrastructure.Specifications;
using Concertable.B2B.Booking.Infrastructure.Strategies;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.B2B.Deal.Contracts;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class BookingWorkflow : IBookingWorkflow
{
    private readonly IBookingRepository bookingRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IUnitOfWorkBehavior unitOfWorkBehavior;
    private readonly IOutboxUnitOfWorkBehavior outboxUnitOfWorkBehavior;
    private readonly IBus bus;
    private readonly IDealStrategyFactory<IConfirmStep> confirmFactory;
    private readonly IDealStrategyFactory<ICancelStep> cancelFactory;
    private readonly IDealStrategyFactory<IContractFactory> contractFactory;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<BookingWorkflow> logger;

    public BookingWorkflow(
        IBookingRepository bookingRepository,
        IUnitOfWork unitOfWork,
        IUnitOfWorkBehavior unitOfWorkBehavior,
        IOutboxUnitOfWorkBehavior outboxUnitOfWorkBehavior,
        IBus bus,
        IDealStrategyFactory<IConfirmStep> confirmFactory,
        IDealStrategyFactory<ICancelStep> cancelFactory,
        IDealStrategyFactory<IContractFactory> contractFactory,
        TimeProvider timeProvider,
        ILogger<BookingWorkflow> logger)
    {
        this.bookingRepository = bookingRepository;
        this.unitOfWork = unitOfWork;
        this.unitOfWorkBehavior = unitOfWorkBehavior;
        this.outboxUnitOfWorkBehavior = outboxUnitOfWorkBehavior;
        this.bus = bus;
        this.confirmFactory = confirmFactory;
        this.cancelFactory = cancelFactory;
        this.contractFactory = contractFactory;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public Task<BookingDto> ConfirmAsync(
        AcceptedApplication application,
        CancellationToken ct = default) =>
        outboxUnitOfWorkBehavior.ExecuteAsync(() => ConfirmCoreAsync(application, ct), ct);

    public Task<UnitResult<CancelBookingError>> CancelAsync(
        int bookingId,
        CancellationToken ct = default) =>
        unitOfWorkBehavior.TryExecuteAsync(
            () => outboxUnitOfWorkBehavior.ExecuteAsync(() => CancelCoreAsync(bookingId, ct), ct),
            exception => exception.IsBookingConcurrencyConflict(bookingId),
            _ => ClassifyCancelConflictAsync(bookingId, ct),
            ct);

    public Task RecordSucceededAsync(
        int bookingId,
        FinancialOperationSucceeded operation,
        CancellationToken ct = default) =>
        unitOfWorkBehavior.ExecuteAsync(() => RecordSucceededCoreAsync(bookingId, operation, ct), ct);

    public Task RecordFailedAsync(
        int bookingId,
        FinancialOperationFailed operation,
        CancellationToken ct = default) =>
        unitOfWorkBehavior.ExecuteAsync(() => RecordFailedCoreAsync(bookingId, operation, ct), ct);

    private async Task<UnitResult<CancelBookingError>> ClassifyCancelConflictAsync(
        int bookingId,
        CancellationToken ct)
    {
        if (await bookingRepository.GetStateByIdAsync(bookingId, ct)
            is BookingState.Cancelled or BookingState.CancellationPending)
            return new Success();

        return new CancelBookingError.Superseded(bookingId);
    }

    private async Task<UnitResult<CancelBookingError>> CancelCoreAsync(
        int bookingId,
        CancellationToken ct)
    {
        var booking = await bookingRepository.GetByIdAsync(bookingId, ct);
        if (booking is null)
            return new CancelBookingError.BookingNotFound(bookingId);
        if (booking.State is BookingState.Cancelled or BookingState.CancellationPending)
            return new Success();
        if (booking.ValidateBeginCancellation().TryGetError(out var transitionError))
            return new CancelBookingError.InvalidTransition(transitionError);

        await cancelFactory.Create(booking.DealType).CancelAsync(booking, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return new Success();
    }

    private async Task<BookingDto> ConfirmCoreAsync(
        AcceptedApplication application,
        CancellationToken ct)
    {
        var snapshot = application.Snapshot;
        var booking = await CreateAsync(
            snapshot,
            (bookingId, createdAtUtc) => CreateContract(
                application,
                bookingId,
                createdAtUtc),
            ct);
        await confirmFactory.Create(booking.DealType).ConfirmAsync(booking, ct);
        await bookingRepository.SaveChangesAsync(ct);
        return booking.ToDto();
    }

    private ContractEntity CreateContract(
        AcceptedApplication application,
        int bookingId,
        DateTime createdAtUtc) =>
        contractFactory
            .Create(application.Snapshot.Contract.Terms.DealType)
            .Create(bookingId, application.Snapshot, createdAtUtc);

    private Task<BookingEntity> CreateAsync(
        ApplicationAcceptanceSnapshot snapshot,
        Func<int, DateTime, ContractEntity> mintContract,
        CancellationToken ct) =>
        unitOfWorkBehavior.ExecuteAsync(() => CreateCoreAsync(snapshot, mintContract, ct), ct);

    private async Task<BookingEntity> CreateCoreAsync(
        ApplicationAcceptanceSnapshot snapshot,
        Func<int, DateTime, ContractEntity> mintContract,
        CancellationToken ct)
    {
        var booking = BookingEntity.Create(snapshot);
        await bookingRepository.AddAsync(booking, ct);
        await bookingRepository.SaveChangesAsync(ct);

        booking.MintContract(mintContract(booking.Id, timeProvider.GetUtcNow().UtcDateTime));
        await bookingRepository.SaveChangesAsync(ct);
        return booking;
    }

    private async Task RecordSucceededCoreAsync(
        int bookingId,
        FinancialOperationSucceeded operation,
        CancellationToken ct)
    {
        var booking = await bookingRepository.GetByIdAsync(bookingId, BookingSpecification.CreateWithContract(), ct);
        if (booking is null || !Matches(bookingId, booking, operation))
        {
            logger.FinancialOutcomeSkipped(operation.Operation, bookingId);
            return;
        }

        if (booking.State == BookingState.CancellationPending)
        {
            await bus.SendAsync(new RefundEscrowCommand(
                booking.Cancellation.OperationId!.Value,
                PaymentOperationReferences.Escrow(bookingId),
                RefundReasonCodes.RequestedByPayer), ct);
            return;
        }
        if (booking.State is BookingState.CancellationFailed or BookingState.Cancelled)
            return;
        if (booking.State == BookingState.Confirmed)
            return;

        if (booking.RecordFinancialConfirmation().TryGetError(out var transitionError))
            throw new InvalidOperationException($"Booking cannot confirm from {transitionError.Current}.");
        await bookingRepository.SaveChangesAsync(ct);
    }

    private async Task RecordFailedCoreAsync(
        int bookingId,
        FinancialOperationFailed operation,
        CancellationToken ct)
    {
        var booking = await bookingRepository.GetByIdAsync(bookingId, ct);
        if (booking is null || !Matches(bookingId, booking, operation))
        {
            logger.FinancialOutcomeSkipped(operation.Operation, bookingId);
            return;
        }

        if (booking.State == BookingState.Confirmed)
            return;
        if (booking.State is BookingState.CancellationFailed or BookingState.Cancelled)
            return;
        if (booking.State == BookingState.CancellationPending)
        {
            if (booking.Cancel().TryGetError(out var transitionError))
                throw new InvalidOperationException($"Booking cannot cancel from {transitionError.Current}.");
            await bookingRepository.SaveChangesAsync(ct);
            return;
        }
        if (IsDuplicateFailure(booking, operation))
            return;

        if (booking.RecordFinancialFailure(operation.Error.Code, operation.Error.Message)
            .TryGetError(out var failureError))
            throw new InvalidOperationException($"Booking cannot record confirmation failure from {failureError.Current}.");
        await bookingRepository.SaveChangesAsync(ct);
    }

    private static bool Matches(
        int bookingId,
        BookingEntity booking,
        FinancialOperationEvidence operation) =>
        booking.ExpectedFinancialOperation == operation.Operation
        && operation switch
        {
            VerifyPaymentSucceededEvidence verified => booking.ApplicationId == verified.ApplicationId,
            VerifyPaymentFailedEvidence failed => booking.ApplicationId == failed.ApplicationId,
            AcceptanceFinancialOperationSucceeded accepted =>
                bookingId == accepted.BookingId && booking.Acceptance.IsHeldBy(accepted.OperationId),
            AcceptanceFinancialOperationRejected rejected =>
                bookingId == rejected.BookingId && booking.Acceptance.IsHeldBy(rejected.OperationId),
            _ => false
        };

    private static bool IsDuplicateFailure(
        BookingEntity booking,
        FinancialOperationFailed operation) =>
        booking.State == BookingState.ConfirmationFailed
        && booking.FinancialFailure?.Code == operation.Error.Code
        && booking.FinancialFailure?.Message == operation.Error.Message;
}
