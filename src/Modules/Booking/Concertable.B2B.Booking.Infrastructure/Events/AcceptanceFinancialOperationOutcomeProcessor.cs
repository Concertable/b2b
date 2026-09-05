using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Application.Models;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.Booking.Infrastructure.Extensions;
using Concertable.B2B.Infrastructure.Payments;
using Concertable.Kernel.DependencyInjection;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts;

namespace Concertable.B2B.Booking.Infrastructure.Events;

internal sealed class AcceptanceFinancialOperationOutcomeProcessor :
    IIntegrationEventHandler<CaptureEscrowSucceededEvent>,
    IIntegrationEventHandler<CaptureEscrowRejectedEvent>,
    IIntegrationEventHandler<DepositEscrowSucceededEvent>,
    IIntegrationEventHandler<DepositEscrowRejectedEvent>
{
    private readonly BookingDbContext context;
    private readonly IBookingService bookingService;
    private readonly IUnitOfWorkBehavior unitOfWorkBehavior;
    private readonly IOutboxUnitOfWorkBehavior outboxUnitOfWorkBehavior;
    private readonly IScoped<AcceptanceFinancialOperationOutcomeProcessor> convergence;

    public AcceptanceFinancialOperationOutcomeProcessor(
        BookingDbContext context,
        IBookingService bookingService,
        IUnitOfWorkBehavior unitOfWorkBehavior,
        IOutboxUnitOfWorkBehavior outboxUnitOfWorkBehavior,
        IScoped<AcceptanceFinancialOperationOutcomeProcessor> convergence)
    {
        this.context = context;
        this.bookingService = bookingService;
        this.unitOfWorkBehavior = unitOfWorkBehavior;
        this.outboxUnitOfWorkBehavior = outboxUnitOfWorkBehavior;
        this.convergence = convergence;
    }

    public Task HandleAsync(
        CaptureEscrowSucceededEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        TryReadBooking(@event.Reference, out var bookingId)
            ? ProcessAsync(
                bookingId,
                envelope,
                new AcceptanceFinancialOperationSucceeded(
                    @event.OperationId,
                    bookingId,
                    FinancialOperation.CaptureEscrow),
                ct)
            : Task.CompletedTask;

    public Task HandleAsync(
        DepositEscrowSucceededEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        TryReadBooking(@event.Reference, out var bookingId)
            ? ProcessAsync(
                bookingId,
                envelope,
                new AcceptanceFinancialOperationSucceeded(
                    @event.OperationId,
                    bookingId,
                    FinancialOperation.DepositEscrow),
                ct)
            : Task.CompletedTask;

    public Task HandleAsync(
        CaptureEscrowRejectedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        TryReadBooking(@event.Reference, out var bookingId)
            ? ProcessAsync(
                bookingId,
                envelope,
                new AcceptanceFinancialOperationRejected(
                    @event.OperationId,
                    bookingId,
                    FinancialOperation.CaptureEscrow,
                    new FinancialOperationError(@event.Code, @event.Message)),
                ct)
            : Task.CompletedTask;

    public Task HandleAsync(
        DepositEscrowRejectedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        TryReadBooking(@event.Reference, out var bookingId)
            ? ProcessAsync(
                bookingId,
                envelope,
                new AcceptanceFinancialOperationRejected(
                    @event.OperationId,
                    bookingId,
                    FinancialOperation.DepositEscrow,
                    new FinancialOperationError(@event.Code, @event.Message)),
                ct)
            : Task.CompletedTask;

    // The outcome can arrive while the venue is cancelling the same booking. The transition already branches
    // on the state it reads, so losing the row is not a failure: the winner moved the booking after this
    // message read it. Rolling the whole message back -- inbox row included -- and reprocessing it in a FRESH
    // scope reads committed truth and lets that branch converge on what won: a captured escrow whose booking
    // is now CancellationPending gets refunded rather than confirmed. Convergence belongs here rather than in
    // the transition because this scope owns the transaction; the rerun does not converge again, so a second
    // loss propagates and the transport redelivers.
    // A reference this service did not mint is another consumer's message, not a malformed one: skipping it
    // leaves the inbox untouched, where parsing it would throw ahead of the inbox row and redeliver forever.
    private static bool TryReadBooking(PaymentOperationReference reference, out int bookingId)
    {
        bookingId = 0;
        return reference.OperationType == PaymentOperationReferences.EscrowType
            && PaymentOperationReferences.TryReadBookingId(reference, out bookingId);
    }

    private Task ProcessAsync(
        int bookingId,
        MessageEnvelope envelope,
        FinancialOperationEvidence evidence,
        CancellationToken ct) =>
        unitOfWorkBehavior.TryExecuteAsync(
            async () =>
            {
                await ProcessCoreAsync(bookingId, envelope, evidence, ct);
                return true;
            },
            exception => exception.IsBookingConcurrencyConflict(bookingId),
            async _ =>
            {
                await convergence.RunAsync(fresh =>
                    fresh.ProcessOnceAsync(bookingId, envelope, evidence, ct));
                return true;
            },
            ct);

    private Task ProcessOnceAsync(
        int bookingId,
        MessageEnvelope envelope,
        FinancialOperationEvidence evidence,
        CancellationToken ct) =>
        unitOfWorkBehavior.ExecuteAsync(() => ProcessCoreAsync(bookingId, envelope, evidence, ct), ct);

    private Task ProcessCoreAsync(
        int bookingId,
        MessageEnvelope envelope,
        FinancialOperationEvidence evidence,
        CancellationToken ct) =>
        outboxUnitOfWorkBehavior.ExecuteAsync(async () =>
        {
            var handler = nameof(AcceptanceFinancialOperationOutcomeProcessor);
            if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, handler, ct))
                return;

            context.AddInboxMessage(envelope, handler);
            await RecordAsync(bookingId, evidence, ct);
        }, ct);

    private Task RecordAsync(
        int bookingId,
        FinancialOperationEvidence evidence,
        CancellationToken ct) =>
        evidence switch
        {
            FinancialOperationSucceeded succeeded =>
                bookingService.RecordSucceededAsync(bookingId, succeeded, ct),
            FinancialOperationFailed failed =>
                bookingService.RecordFailedAsync(bookingId, failed, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(evidence), evidence, null)
        };
}
