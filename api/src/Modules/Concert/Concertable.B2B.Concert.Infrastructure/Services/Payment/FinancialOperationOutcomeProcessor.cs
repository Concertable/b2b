using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.DataAccess.Application;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal sealed class FinancialOperationOutcomeProcessor :
    IIntegrationEventHandler<RefundEscrowSucceededEvent>,
    IIntegrationEventHandler<RefundEscrowDeferredEvent>,
    IIntegrationEventHandler<RefundEscrowRejectedEvent>
{
    private readonly ConcertDbContext context;
    private readonly IConcertReadDbContext readDbContext;
    private readonly ITenantScope tenantScope;
    private readonly IOutboxUnitOfWorkBehavior outboxUnitOfWorkBehavior;

    public FinancialOperationOutcomeProcessor(
        ConcertDbContext context,
        IConcertReadDbContext readDbContext,
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

    private async Task ProcessAsync(
        Guid operationId,
        MessageEnvelope envelope,
        Func<ConcertEntity, Task> action,
        CancellationToken ct)
    {
        // A refund outcome names only its operation, so the owner comes off the row itself through the
        // unfiltered read stance; the transition then runs as that tenant, where the filter can see it.
        var venueTenantId = await readDbContext.Concerts
            .Where(value => value.CancellationOperationId == operationId)
            .Select(value => (Guid?)value.VenueTenantId)
            .SingleOrDefaultAsync(ct);

        using var acting = venueTenantId is null ? null : tenantScope.As(venueTenantId.Value);
        await outboxUnitOfWorkBehavior.ExecuteAsync(async () =>
        {
            var handler = nameof(FinancialOperationOutcomeProcessor);
            if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, handler, ct))
                return;

            context.AddInboxMessage(envelope, handler);
            if (venueTenantId is null)
                return;

            var concert = await context.Concerts
                .SingleOrDefaultAsync(value => value.CancellationOperationId == operationId, ct);
            if (concert is not null)
                await action(concert);
        }, ct);
    }
}
