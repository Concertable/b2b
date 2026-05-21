using Concertable.Messaging;
using Concertable.Shared;

namespace Concertable.Artist.Contracts.Events;

public record ArtistChangedEvent(
    int ArtistId,
    Guid UserId,
    string Name,
    string Avatar,
    string BannerUrl,
    string County,
    string Town,
    double Latitude,
    double Longitude,
    string Email,
    IReadOnlyCollection<Genre> Genres) : IIntegrationEvent;
