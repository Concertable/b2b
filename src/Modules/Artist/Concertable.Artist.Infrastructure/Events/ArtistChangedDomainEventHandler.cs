using Concertable.Artist.Contracts.Events;
using Concertable.Artist.Domain.Events;

namespace Concertable.Artist.Infrastructure.Events;

internal class ArtistChangedDomainEventHandler(IBus bus)
    : IPreCommitDomainEventHandler<ArtistChangedDomainEvent>
{
    public Task HandleAsync(ArtistChangedDomainEvent e, CancellationToken ct = default)
    {
        var artist = e.Artist;
        return bus.PublishAsync(new ArtistChangedEvent(
            artist.Id,
            artist.UserId,
            artist.Name,
            artist.Avatar,
            artist.BannerUrl,
            artist.Address.County,
            artist.Address.Town,
            artist.Location.Y,
            artist.Location.X,
            artist.Email,
            artist.ArtistGenres.Select(g => g.Genre).ToArray()), ct);
    }
}
