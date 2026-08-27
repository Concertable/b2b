using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.Kernel;

namespace Concertable.B2B.Booking.Infrastructure.Events;

internal sealed class ApplicationAcceptedDomainEventHandler
    : IPreCommitDomainEventHandler<ApplicationAcceptedDomainEvent>
{
    private readonly IConfirmationExecutor confirmation;

    public ApplicationAcceptedDomainEventHandler(IConfirmationExecutor confirmation)
    {
        this.confirmation = confirmation;
    }

    public Task HandleAsync(
        ApplicationAcceptedDomainEvent @event,
        CancellationToken ct = default) =>
        this.confirmation.ExecuteAsync(@event.Application, ct);
}
