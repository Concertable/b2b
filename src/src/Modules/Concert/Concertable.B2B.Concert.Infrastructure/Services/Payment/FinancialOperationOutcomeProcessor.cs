using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal sealed class FinancialOperationOutcomeProcessor :
    IIntegrationEventHandler<CaptureEscrowSucceededEvent>,
    IIntegrationEventHandler<CaptureEscrowRejectedEvent>,
    IIntegrationEventHandler<DepositEscrowSucceededEvent>,
    IIntegrationEventHandler<DepositEscrowRejectedEvent>,
    IIntegrationEventHandler<RefundEscrowSucceededEvent>,
    IIntegrationEventHandler<RefundEscrowRejectedEvent>,
    IIntegrationEventHandler<RefundEscrowDeferredEvent>
{
    private readonly ConcertDbContext context;
    private readonly IEscrowExecutor escrowExecutor;
    private readonly ILifecycleTransitioner transitioner;
    private readonly IOutboxUnitOfWorkBehavior outbox;

    public FinancialOperationOutcomeProcessor(
        ConcertDbContext context,
        IEscrowExecutor escrowExecutor,
        ILifecycleTransitioner transitioner,
        IOutboxUnitOfWorkBehavior outbox)
    {
        this.context = context;
        this.escrowExecutor = escrowExecutor;
        this.transitioner = transitioner;
        this.outbox = outbox;
    }

    public Task HandleAsync(
        CaptureEscrowSucceededEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        AcceptanceSucceededAsync(@event.OperationId, @event.BookingId, envelope, ct);

    public Task HandleAsync(
        DepositEscrowSucceededEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        AcceptanceSucceededAsync(@event.OperationId, @event.BookingId, envelope, ct);

    public Task HandleAsync(
        CaptureEscrowRejectedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        AcceptanceRejectedAsync(@event.OperationId, @event.Code, @event.Message, envelope, ct);

    public Task HandleAsync(
        DepositEscrowRejectedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        AcceptanceRejectedAsync(@event.OperationId, @event.Code, @event.Message, envelope, ct);

    public Task HandleAsync(
        RefundEscrowSucceededEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        RefundSucceededAsync(@event.OperationId, envelope, ct);

    public Task HandleAsync(
        RefundEscrowRejectedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        RefundRejectedAsync(@event.OperationId, @event.Code, @event.Message, envelope, ct);

    public Task HandleAsync(
        RefundEscrowDeferredEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        RecordDeferredAsync(@event.OperationId, envelope, ct);

    private Task AcceptanceSucceededAsync(
        Guid operationId,
        int bookingId,
        MessageEnvelope envelope,
        CancellationToken ct) =>
        ProcessAsync(envelope, async () =>
        {
            var application = await context.Applications
                .SingleOrDefaultAsync(value => value.AcceptanceOperationId == operationId, ct)
                ?? throw new InvalidOperationException($"Acceptance operation {operationId} has no application.");
            if (application.State is LifecycleState.Booked or LifecycleState.Cancelled)
                return;

            await escrowExecutor.SucceededAsync(bookingId, ct);
        }, ct);

    private Task AcceptanceRejectedAsync(
        Guid operationId,
        string code,
        string message,
        MessageEnvelope envelope,
        CancellationToken ct) =>
        ProcessAsync(envelope, async () =>
        {
            var application = await context.Applications
                .Include(value => value.Booking)
                .SingleOrDefaultAsync(value => value.AcceptanceOperationId == operationId, ct)
                ?? throw new InvalidOperationException($"Acceptance operation {operationId} has no application.");
            if (application.State is LifecycleState.PaymentFailed or LifecycleState.Cancelled)
                return;

            if (application.State == LifecycleState.CancellationPending)
            {
                await transitioner.TransitionAsync(application.Id, Trigger.RefundSucceeded, ct: ct)
                    .GetValueOrThrowAsync();
                return;
            }

            application.RecordFinancialFailure(code, message);
            await escrowExecutor.FailedAsync(application.Booking?.Id
                ?? throw new InvalidOperationException($"Acceptance operation {operationId} has no booking."), ct);
        }, ct);

    private Task RefundSucceededAsync(
        Guid operationId,
        MessageEnvelope envelope,
        CancellationToken ct) =>
        ProcessAsync(envelope, async () =>
        {
            var application = await LoadCancellationAsync(operationId, ct);
            if (application.State == LifecycleState.Cancelled)
                return;

            await transitioner.TransitionAsync(
                application.Id,
                Trigger.RefundSucceeded,
                app =>
                {
                    app.Booking?.Concert?.Cancel();
                    return Task.CompletedTask;
                },
                ct).GetValueOrThrowAsync();
        }, ct);

    private Task RefundRejectedAsync(
        Guid operationId,
        string code,
        string message,
        MessageEnvelope envelope,
        CancellationToken ct) =>
        ProcessAsync(envelope, async () =>
        {
            var application = await LoadCancellationAsync(operationId, ct);
            if (application.State == LifecycleState.CancellationFailed)
                return;

            application.RecordFinancialFailure(code, message);
            await transitioner.TransitionAsync(application.Id, Trigger.RefundFailed, ct: ct)
                .GetValueOrThrowAsync();
        }, ct);

    private Task RecordDeferredAsync(
        Guid operationId,
        MessageEnvelope envelope,
        CancellationToken ct) =>
        ProcessAsync(envelope, async () =>
        {
            var application = await LoadCancellationAsync(operationId, ct);
            if (application.State != LifecycleState.CancellationPending)
                throw new InvalidOperationException(
                    $"Deferred refund {operationId} found application {application.Id} in {application.State}.");
        }, ct);

    private async Task<ApplicationEntity> LoadCancellationAsync(Guid operationId, CancellationToken ct) =>
        await context.Applications
            .Include(value => value.Booking)
                .ThenInclude(value => value!.Concert)
            .SingleOrDefaultAsync(value => value.CancellationOperationId == operationId, ct)
            ?? throw new InvalidOperationException($"Cancellation operation {operationId} has no application.");

    private Task ProcessAsync(MessageEnvelope envelope, Func<Task> action, CancellationToken ct) =>
        outbox.ExecuteAsync(async () =>
        {
            var handler = nameof(FinancialOperationOutcomeProcessor);
            if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, handler, ct))
                return;

            context.AddInboxMessage(envelope, handler);
            await action();
        }, ct);
}
