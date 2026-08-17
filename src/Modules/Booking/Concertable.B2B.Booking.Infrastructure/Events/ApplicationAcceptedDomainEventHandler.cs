using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.Kernel;

namespace Concertable.B2B.Booking.Infrastructure.Events;

internal sealed class ApplicationAcceptedDomainEventHandler
    : IPreCommitDomainEventHandler<ApplicationAcceptedDomainEvent>
{
    private readonly IStepResolver<IConfirmStep> steps;
    private readonly IOutboxUnitOfWorkBehavior outbox;

    public ApplicationAcceptedDomainEventHandler(
        IStepResolver<IConfirmStep> steps,
        IOutboxUnitOfWorkBehavior outbox)
    {
        this.steps = steps;
        this.outbox = outbox;
    }

    public Task HandleAsync(
        ApplicationAcceptedDomainEvent @event,
        CancellationToken ct = default) =>
        this.outbox.ExecuteAsync(
            () => this.steps.Resolve(@event.Application.DealType)
                .ExecuteAsync(@event.Application, ct),
            ct);
}
