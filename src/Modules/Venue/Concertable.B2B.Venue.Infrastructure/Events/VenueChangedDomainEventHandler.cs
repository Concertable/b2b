using Concertable.Kernel;
using Concertable.Messaging.Contracts;
using Concertable.B2B.Venue.Contracts.Events;
using Concertable.B2B.Venue.Domain.Events;

namespace Concertable.B2B.Venue.Infrastructure.Events;

internal sealed class VenueChangedDomainEventHandler
    : IPreCommitDomainEventHandler<VenueChangedDomainEvent>
{
    private readonly IBus bus;

    public VenueChangedDomainEventHandler(IBus bus)
    {
        this.bus = bus;
    }

    public Task HandleAsync(VenueChangedDomainEvent e, CancellationToken ct = default)
    {
        var venue = e.Venue;
        return bus.PublishAsync(new VenueChangedEvent(
            venue.Id,
            venue.UserId,
            venue.Name,
            venue.About,
            venue.Avatar,
            venue.BannerUrl,
            venue.Address.County,
            venue.Address.Town,
            venue.Location.Y,
            venue.Location.X,
            venue.Email), ct);
    }
}
