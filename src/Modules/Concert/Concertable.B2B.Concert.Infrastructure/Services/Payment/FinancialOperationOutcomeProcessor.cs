using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal sealed class FinancialOperationOutcomeProcessor :
    IIntegrationEventHandler<RefundEscrowSucceededEvent>,
    IIntegrationEventHandler<RefundEscrowDeferredEvent>,
    IIntegrationEventHandler<RefundEscrowRejectedEvent>
{
    private readonly ConcertDbContext context;
    private readonly IOutboxUnitOfWorkBehavior outboxUnitOfWorkBehavior;

    public FinancialOperationOutcomeProcessor(
        ConcertDbContext context,
        IOutboxUnitOfWorkBehavior outboxUnitOfWorkBehavior)
    {
        this.context = context;
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
        ProcessAsync(@event.OperationId, envelope, Cancel, ct);

    public Task HandleAsync(
        RefundEscrowRejectedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        ProcessAsync(@event.OperationId, envelope, concert =>
        {
            if (concert.State is ConcertState.CancellationFailed or ConcertState.Cancelled)
                return Task.CompletedTask;

            if (concert.RecordCancellationFailure(@event.Code, @event.Message).TryGetError(out var transitionError))
                throw new InvalidOperationException($"Concert cannot record cancellation failure from {transitionError.Current}.");
            return Task.CompletedTask;
        }, ct);

    private static Task Cancel(ConcertEntity concert)
    {
        if (concert.State is ConcertState.Cancelled)
            return Task.CompletedTask;

        if (concert.Cancel().TryGetError(out var transitionError))
            throw new InvalidOperationException($"Concert cannot cancel from {transitionError.Current}.");
        return Task.CompletedTask;
    }

    private Task ProcessAsync(
        Guid operationId,
        MessageEnvelope envelope,
        Func<ConcertEntity, Task> action,
        CancellationToken ct) =>
        outboxUnitOfWorkBehavior.ExecuteAsync(async () =>
        {
            var handler = nameof(FinancialOperationOutcomeProcessor);
            if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, handler, ct))
                return;

            context.AddInboxMessage(envelope, handler);
            var concert = await context.Concerts
                .SingleOrDefaultAsync(value => value.CancellationOperationId == operationId, ct);
            if (concert is not null)
                await action(concert);
        }, ct);
}
