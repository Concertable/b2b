using Concertable.User.Contracts.Events;
using Concertable.User.Domain.Events;
using Concertable.Shared;

namespace Concertable.User.Infrastructure.Events;

internal class UserCreatedDomainEventHandler(IBus bus)
    : IPreCommitDomainEventHandler<UserCreatedDomainEvent>
{
    public Task HandleAsync(UserCreatedDomainEvent e, CancellationToken ct = default)
    {
        IIntegrationEvent evt = e.User.Role switch
        {
            Role.Customer => new CustomerRegisteredEvent(e.User.Id, e.User.Email),
            Role.VenueManager => new VenueManagerRegisteredEvent(e.User.Id, e.User.Email),
            Role.ArtistManager => new ArtistManagerRegisteredEvent(e.User.Id, e.User.Email),
            Role.Admin => new AdminRegisteredEvent(e.User.Id, e.User.Email),
            _ => throw new ArgumentOutOfRangeException(nameof(e.User.Role))
        };
        return bus.PublishAsync(evt, ct);
    }
}
