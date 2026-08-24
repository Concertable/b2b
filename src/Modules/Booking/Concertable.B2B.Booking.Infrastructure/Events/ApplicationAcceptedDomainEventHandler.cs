using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.Kernel;

namespace Concertable.B2B.Booking.Infrastructure.Events;

internal sealed class ApplicationAcceptedDomainEventHandler
    : IPreCommitDomainEventHandler<ApplicationAcceptedDomainEvent>
{
    private readonly IBookingConfirmationExecutor confirmation;

    public ApplicationAcceptedDomainEventHandler(IBookingConfirmationExecutor confirmation)
    {
        this.confirmation = confirmation;
    }

    public Task HandleAsync(
        ApplicationAcceptedDomainEvent @event,
        CancellationToken ct = default) =>
        this.confirmation.ExecuteAsync(@event.Application, ct);
}
