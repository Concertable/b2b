using Concertable.User.Contracts.Events;
using Concertable.User.Domain.Events;
using Concertable.Shared;

namespace Concertable.User.Infrastructure.Events;

internal class UserCreatedDomainEventHandler(IBus bus)
    : IPreCommitDomainEventHandler<UserCreatedDomainEvent>
{
    public Task HandleAsync(UserCreatedDomainEvent e, CancellationToken ct = default) =>
        e.User.Role switch
        {
            Role.Customer => bus.PublishAsync(new CustomerRegisteredEvent(e.User.Id, e.User.Email), ct),
            Role.VenueManager => bus.PublishAsync(new VenueManagerRegisteredEvent(e.User.Id, e.User.Email), ct),
            Role.ArtistManager => bus.PublishAsync(new ArtistManagerRegisteredEvent(e.User.Id, e.User.Email), ct),
            Role.Admin => bus.PublishAsync(new AdminRegisteredEvent(e.User.Id, e.User.Email), ct),
            _ => throw new ArgumentOutOfRangeException(nameof(e.User.Role))
        };
}
