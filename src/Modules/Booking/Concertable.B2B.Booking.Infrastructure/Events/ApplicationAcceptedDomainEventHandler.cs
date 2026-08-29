using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.Kernel;

namespace Concertable.B2B.Booking.Infrastructure.Events;

internal sealed class ApplicationAcceptedDomainEventHandler
    : IPreCommitDomainEventHandler<ApplicationAcceptedDomainEvent>
{
    private readonly IBookingWorkflow workflow;

    public ApplicationAcceptedDomainEventHandler(IBookingWorkflow workflow)
    {
        this.workflow = workflow;
    }

    public Task HandleAsync(
        ApplicationAcceptedDomainEvent @event,
        CancellationToken ct = default) =>
        this.workflow.ConfirmAsync(@event.Application, ct);
}
