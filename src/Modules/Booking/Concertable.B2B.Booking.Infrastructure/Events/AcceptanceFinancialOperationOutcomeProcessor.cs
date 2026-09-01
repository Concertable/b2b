using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Application.Models;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.Booking.Infrastructure.Extensions;
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
    private readonly IBookingService bookings;
    private readonly IUnitOfWorkBehavior unitOfWork;
    private readonly IOutboxUnitOfWorkBehavior outboxBehavior;
    private readonly IScoped<AcceptanceFinancialOperationOutcomeProcessor> convergence;

    public AcceptanceFinancialOperationOutcomeProcessor(
        BookingDbContext context,
        IBookingService bookings,
        IUnitOfWorkBehavior unitOfWork,
        IOutboxUnitOfWorkBehavior outboxBehavior,
        IScoped<AcceptanceFinancialOperationOutcomeProcessor> convergence)
    {
        this.context = context;
        this.bookings = bookings;
        this.unitOfWork = unitOfWork;
        this.outboxBehavior = outboxBehavior;
        this.convergence = convergence;
    }

    public Task HandleAsync(
        CaptureEscrowSucceededEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        ProcessAsync(
            @event.BookingId,
            envelope,
            new AcceptanceFinancialOperationSucceeded(
                @event.OperationId,
                @event.BookingId,
                FinancialOperation.CaptureEscrow,
                @event.ReferenceId),
            ct);

    public Task HandleAsync(
        DepositEscrowSucceededEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        ProcessAsync(
            @event.BookingId,
            envelope,
            new AcceptanceFinancialOperationSucceeded(
                @event.OperationId,
                @event.BookingId,
                FinancialOperation.DepositEscrow,
                @event.ReferenceId),
            ct);

    public Task HandleAsync(
        CaptureEscrowRejectedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        ProcessAsync(
            @event.BookingId,
            envelope,
            new AcceptanceFinancialOperationRejected(
                @event.OperationId,
                @event.BookingId,
                FinancialOperation.CaptureEscrow,
                new FinancialOperationError(@event.Code, @event.Message)),
            ct);

    public Task HandleAsync(
        DepositEscrowRejectedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        ProcessAsync(
            @event.BookingId,
            envelope,
            new AcceptanceFinancialOperationRejected(
                @event.OperationId,
                @event.BookingId,
                FinancialOperation.DepositEscrow,
                new FinancialOperationError(@event.Code, @event.Message)),
            ct);

    // The outcome can arrive while the venue is cancelling the same booking. The transition already branches
    // on the state it reads, so losing the row is not a failure: the winner moved the booking after this
    // message read it. Rolling the whole message back -- inbox row included -- and reprocessing it in a FRESH
    // scope reads committed truth and lets that branch converge on what won: a captured escrow whose booking
    // is now CancellationPending gets refunded rather than confirmed. Convergence belongs here rather than in
    // the transition because this scope owns the transaction; the rerun does not converge again, so a second
    // loss propagates and the transport redelivers.
    private Task ProcessAsync(
        int bookingId,
        MessageEnvelope envelope,
        FinancialOperationEvidence evidence,
        CancellationToken ct) =>
        unitOfWork.TryExecuteAsync(
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
        unitOfWork.ExecuteAsync(() => ProcessCoreAsync(bookingId, envelope, evidence, ct), ct);

    private Task ProcessCoreAsync(
        int bookingId,
        MessageEnvelope envelope,
        FinancialOperationEvidence evidence,
        CancellationToken ct) =>
        outboxBehavior.ExecuteAsync(async () =>
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
                bookings.RecordSucceededAsync(bookingId, succeeded, ct),
            FinancialOperationFailed failed =>
                bookings.RecordFailedAsync(bookingId, failed, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(evidence), evidence, null)
        };
}
