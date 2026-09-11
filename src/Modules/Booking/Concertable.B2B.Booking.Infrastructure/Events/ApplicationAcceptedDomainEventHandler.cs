using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.Kernel;

namespace Concertable.B2B.Booking.Infrastructure.Events;

internal sealed class ApplicationAcceptedDomainEventHandler
    : IPreCommitDomainEventHandler<ApplicationAcceptedDomainEvent>
{
    private readonly IBookingWorkflow bookingWorkflow;

    public ApplicationAcceptedDomainEventHandler(IBookingWorkflow bookingWorkflow)
    {
        this.bookingWorkflow = bookingWorkflow;
    }

    public Task HandleAsync(
        ApplicationAcceptedDomainEvent @event,
        CancellationToken ct = default) =>
        bookingWorkflow.ConfirmAsync(@event.Application, ct);
}
