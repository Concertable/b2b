using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Events;

internal sealed class CancellationFinancialOperationOutcomeProcessor :
    IIntegrationEventHandler<RefundEscrowSucceededEvent>,
    IIntegrationEventHandler<RefundEscrowRejectedEvent>
{
    private readonly BookingDbContext context;
    private readonly IOutboxUnitOfWorkBehavior outbox;

    public CancellationFinancialOperationOutcomeProcessor(
        BookingDbContext context,
        IOutboxUnitOfWorkBehavior outbox)
    {
        this.context = context;
        this.outbox = outbox;
    }

    public Task HandleAsync(
        RefundEscrowSucceededEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        ProcessAsync(@event.OperationId, envelope, booking =>
        {
            if (booking.State == State.Cancelled)
                return;

            if (booking.Cancel().TryGetError(out var transitionError))
                throw new InvalidOperationException($"Booking cannot cancel from {transitionError.Current}.");
        }, ct);

    public Task HandleAsync(
        RefundEscrowRejectedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        ProcessAsync(@event.OperationId, envelope, booking =>
        {
            if (booking.State == State.CancellationFailed)
                return;

            if (booking.RecordCancellationFailure(@event.Code, @event.Message).TryGetError(out var transitionError))
                throw new InvalidOperationException($"Booking cannot record cancellation failure from {transitionError.Current}.");
        }, ct);

    private Task ProcessAsync(
        Guid operationId,
        MessageEnvelope envelope,
        Action<BookingEntity> action,
        CancellationToken ct) =>
        outbox.ExecuteAsync(async () =>
        {
            var handler = nameof(CancellationFinancialOperationOutcomeProcessor);
            if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, handler, ct))
                return;

            context.AddInboxMessage(envelope, handler);
            var booking = await context.Bookings
                .SingleOrDefaultAsync(value => value.CancellationOperationId == operationId, ct);
            if (booking is not null)
                action(booking);
        }, ct);
}
