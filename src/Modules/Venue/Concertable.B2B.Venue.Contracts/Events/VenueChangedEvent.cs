using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Venue.Contracts.Events;

public record VenueChangedEvent(
    int VenueId,
    Guid UserId,
    string Name,
    string About,
    string Avatar,
    string BannerUrl,
    string County,
    string Town,
    double Latitude,
    double Longitude,
    string Email) : IIntegrationEvent;
