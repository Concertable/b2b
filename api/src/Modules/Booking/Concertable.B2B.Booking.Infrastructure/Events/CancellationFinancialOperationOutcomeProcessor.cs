using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.DataAccess.Application;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Events;

internal sealed class CancellationFinancialOperationOutcomeProcessor :
    IIntegrationEventHandler<RefundEscrowSucceededEvent>,
    IIntegrationEventHandler<RefundEscrowDeferredEvent>,
    IIntegrationEventHandler<RefundEscrowRejectedEvent>
{
    private readonly BookingDbContext context;
    private readonly IBookingReadDbContext readDbContext;
    private readonly ITenantScope tenantScope;
    private readonly IOutboxUnitOfWorkBehavior outboxUnitOfWorkBehavior;

    public CancellationFinancialOperationOutcomeProcessor(
        BookingDbContext context,
        IBookingReadDbContext readDbContext,
        ITenantScope tenantScope,
        IOutboxUnitOfWorkBehavior outboxUnitOfWorkBehavior)
    {
        this.context = context;
        this.readDbContext = readDbContext;
        this.tenantScope = tenantScope;
        this.outboxUnitOfWorkBehavior = outboxUnitOfWorkBehavior;
    }

    public Task HandleAsync(
        RefundEscrowSucceededEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        ProcessAsync(@event.OperationId, envelope, Cancel, ct);

    public Task HandleAsync(
        RefundEscrowDeferredEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        outboxUnitOfWorkBehavior.ExecuteAsync(() => TryRecordInboxAsync(envelope, ct), ct);

    public Task HandleAsync(
        RefundEscrowRejectedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        ProcessAsync(@event.OperationId, envelope, booking =>
        {
            if (booking.State is BookingState.CancellationFailed or BookingState.Cancelled)
                return;

            if (booking.RecordCancellationFailure(@event.Code, @event.Message).TryGetError(out var transitionError))
                throw new InvalidOperationException($"Booking cannot record cancellation failure from {transitionError.Current}.");
        }, ct);

    private static void Cancel(BookingEntity booking)
    {
        if (booking.State == BookingState.Cancelled)
            return;

        if (booking.Cancel().TryGetError(out var transitionError))
            throw new InvalidOperationException($"Booking cannot cancel from {transitionError.Current}.");
    }

    private async Task ProcessAsync(
        Guid operationId,
        MessageEnvelope envelope,
        Action<BookingEntity> action,
        CancellationToken ct)
    {
        // A refund outcome names only its operation, so the owner comes off the row itself through the
        // unfiltered read stance; the transition then runs as that tenant, where the filter can see it.
        var venueTenantId = await readDbContext.Bookings
            .Where(value => value.CancellationOperationId == operationId)
            .Select(value => (Guid?)value.VenueTenantId)
            .SingleOrDefaultAsync(ct);
        if (venueTenantId is null)
        {
            await outboxUnitOfWorkBehavior.ExecuteAsync(() => TryRecordInboxAsync(envelope, ct), ct);
            return;
        }

        using var acting = tenantScope.As(venueTenantId.Value);
        await outboxUnitOfWorkBehavior.ExecuteAsync(async () =>
        {
            if (!await TryRecordInboxAsync(envelope, ct))
                return;

            var booking = await context.Bookings
                .SingleOrDefaultAsync(value => value.CancellationOperationId == operationId, ct);
            if (booking is not null)
                action(booking);
        }, ct);
    }

    private async Task<bool> TryRecordInboxAsync(MessageEnvelope envelope, CancellationToken ct)
    {
        var handler = nameof(CancellationFinancialOperationOutcomeProcessor);
        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, handler, ct))
            return false;

        context.AddInboxMessage(envelope, handler);
        return true;
    }
}
