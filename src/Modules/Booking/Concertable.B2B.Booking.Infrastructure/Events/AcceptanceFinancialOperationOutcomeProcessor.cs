using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Application.Models;
using Concertable.B2B.Booking.Domain.State;
using Concertable.B2B.Booking.Infrastructure.Data;
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
    private readonly IOutboxUnitOfWorkBehavior outbox;

    public AcceptanceFinancialOperationOutcomeProcessor(
        BookingDbContext context,
        IBookingService bookings,
        IOutboxUnitOfWorkBehavior outbox)
    {
        this.context = context;
        this.bookings = bookings;
        this.outbox = outbox;
    }

    public Task HandleAsync(
        CaptureEscrowSucceededEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        SucceededAsync(
            @event.OperationId,
            @event.BookingId,
            FinancialOperation.CaptureEscrow,
            @event.ReferenceId,
            envelope,
            ct);

    public Task HandleAsync(
        DepositEscrowSucceededEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        SucceededAsync(
            @event.OperationId,
            @event.BookingId,
            FinancialOperation.DepositEscrow,
            @event.ReferenceId,
            envelope,
            ct);

    public Task HandleAsync(
        CaptureEscrowRejectedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        RejectedAsync(
            @event.OperationId,
            @event.BookingId,
            FinancialOperation.CaptureEscrow,
            @event.Code,
            @event.Message,
            envelope,
            ct);

    public Task HandleAsync(
        DepositEscrowRejectedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        RejectedAsync(
            @event.OperationId,
            @event.BookingId,
            FinancialOperation.DepositEscrow,
            @event.Code,
            @event.Message,
            envelope,
            ct);

    private Task SucceededAsync(
        Guid operationId,
        int bookingId,
        FinancialOperation operation,
        string providerReferenceId,
        MessageEnvelope envelope,
        CancellationToken ct) =>
        ProcessAsync(
            envelope,
            () => bookings.RecordSucceededAsync(
                bookingId,
                new AcceptanceFinancialOperationSucceeded(
                    operationId,
                    bookingId,
                    operation,
                    providerReferenceId),
                ct),
            ct);

    private Task RejectedAsync(
        Guid operationId,
        int bookingId,
        FinancialOperation operation,
        string code,
        string message,
        MessageEnvelope envelope,
        CancellationToken ct) =>
        ProcessAsync(
            envelope,
            () => bookings.RecordFailedAsync(
                bookingId,
                new AcceptanceFinancialOperationRejected(
                    operationId,
                    bookingId,
                    operation,
                    new FinancialOperationError(code, message)),
                ct),
            ct);

    private Task ProcessAsync(
        MessageEnvelope envelope,
        Func<Task> action,
        CancellationToken ct) =>
        outbox.ExecuteAsync(async () =>
        {
            var handler = nameof(AcceptanceFinancialOperationOutcomeProcessor);
            if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, handler, ct))
                return;

            context.AddInboxMessage(envelope, handler);
            await action();
        }, ct);
}
