using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Artist.Domain.Events;
using Concertable.Kernel;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Artist.Infrastructure.Events;

internal sealed class ArtistChangedDomainEventHandler(IBus bus)
    : IPreCommitDomainEventHandler<ArtistChangedDomainEvent>
{
    public Task HandleAsync(ArtistChangedDomainEvent e, CancellationToken ct = default)
    {
        var artist = e.Artist;
        return bus.PublishAsync(new ArtistChangedEvent(
            artist.Id,
            artist.UserId,
            artist.Name,
            artist.About,
            artist.Avatar,
            artist.BannerUrl,
            artist.Address.County,
            artist.Address.Town,
            artist.Location.Y,
            artist.Location.X,
            artist.Email,
            artist.Genres.ToArray()), ct);
    }
}
