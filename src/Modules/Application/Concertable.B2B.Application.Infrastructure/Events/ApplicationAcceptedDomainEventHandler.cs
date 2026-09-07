using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Contracts.Events;
using Concertable.Kernel;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Application.Infrastructure.Events;

internal sealed class ApplicationAcceptedDomainEventHandler
    : IPreCommitDomainEventHandler<ApplicationAcceptedDomainEvent>
{
    private readonly IBus bus;

    public ApplicationAcceptedDomainEventHandler(IBus bus)
    {
        this.bus = bus;
    }

    public Task HandleAsync(ApplicationAcceptedDomainEvent e, CancellationToken ct = default) =>
        bus.PublishAsync(
            new ApplicationAcceptedEvent(
                e.Application.Snapshot.Application.Opportunity.Id,
                e.Application.Snapshot.Application.Opportunity.Venue.TenantId),
            ct);
}
